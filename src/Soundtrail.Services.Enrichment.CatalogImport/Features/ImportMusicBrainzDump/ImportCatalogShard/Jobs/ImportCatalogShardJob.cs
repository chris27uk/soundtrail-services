using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Catalog.MusicBrainzDumpImport;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard.Ports;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard.Work;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.ImportCatalogShard.Ports;
using Soundtrail.Services.Enrichment.CatalogImport.Infrastructure.Lease;
using Soundtrail.Services.Enrichment.CatalogImport.Infrastructure.Telemetry;

namespace Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.ImportCatalogShard;

public sealed class ImportCatalogShardJob(
    IMusicBrainzDumpImportJobStore jobStore,
    ICatalogImportLeaseOwner leaseOwner,
    IMusicBrainzDumpShardStore shardStore,
    IMusicBrainzArtistDumpRowMapper artistRowMapper,
    IMusicBrainzReleaseGroupDumpRowMapper releaseGroupRowMapper,
    IMusicBrainzTrackDumpRowMapper trackRowMapper,
    ICatalogDumpBatchWriter batchWriter,
    IDownloadDumpAndShardWorkQueue downloadWorkQueue,
    IOptions<MusicBrainzDumpOptions> options,
    ILogger<ImportCatalogShardJob> logger) : IImportCatalogShardJob
{
    public async Task RunAsync(
        MusicBrainzDumpImportJobId jobId,
        MusicBrainzDumpImportPhase phase,
        int shardId,
        CancellationToken cancellationToken = default)
    {
        if (phase is not (
            MusicBrainzDumpImportPhase.Artists or
            MusicBrainzDumpImportPhase.ReleaseGroups or
            MusicBrainzDumpImportPhase.Recordings))
        {
            logger.LogWarning(
                "Skipping unsupported dump phase {Phase} for job {JobId} shard {ShardId}.",
                phase,
                jobId.Value,
                shardId);
            return;
        }

        var leaseDuration = options.Value.LeaseDuration;
        var job = await TryBeginShardAsync(jobId, phase, shardId, leaseDuration, cancellationToken);
        if (job is null)
        {
            return;
        }

        using var activity = MusicBrainzDumpImportTelemetry.StartShardImportActivity(job, phase, shardId);

        var shard = job.GetOrAddShard(phase, shardId);
        var dumpObservedAt = ResolveDumpObservedAt(job);
        var batchSize = Math.Max(1, options.Value.BulkInsertBatchSize);
        var processed = shard.LineOffset;
        var imported = 0;
        var skipped = 0;
        var buffer = new List<CatalogDumpBatchItem>(batchSize);
        var touchedArtists = new HashSet<ArtistId>();

        await foreach (var line in shardStore.ReadShardLinesAsync(
                           jobId,
                           phase,
                           shardId,
                           skipLines: processed,
                           cancellationToken))
        {
            processed++;
            var item = TryMap(phase, line);
            if (item is null)
            {
                skipped++;
            }
            else
            {
                buffer.Add(item);
                imported++;
            }

            if (buffer.Count >= batchSize)
            {
                job = await FlushBufferAsync(
                    job,
                    phase,
                    shardId,
                    buffer,
                    dumpObservedAt,
                    processed,
                    leaseDuration,
                    touchedArtists,
                    cancellationToken);
            }
        }

        if (buffer.Count > 0)
        {
            job = await FlushBufferAsync(
                job,
                phase,
                shardId,
                buffer,
                dumpObservedAt,
                processed,
                leaseDuration,
                touchedArtists,
                cancellationToken);
        }

        await batchWriter.ProjectArtistsAsync(touchedArtists, dumpObservedAt, cancellationToken);

        job = await PersistOwnedShardAsync(
            job,
            phase,
            shardId,
            processed,
            leaseDuration,
            markCompleted: true,
            cancellationToken);

        MusicBrainzDumpImportTelemetry.RecordRows(job.Id.Value, imported, skipped);

        var (completedShards, totalShards) = MusicBrainzDumpImportProgress.CountPhaseShards(job, phase);
        if (totalShards > 0)
        {
            MusicBrainzDumpImportTelemetry.RecordProgress(
                job,
                MusicBrainzDumpImportProgress.AfterShardCompleted(phase, completedShards, totalShards));
        }

        await PersistAfterShardWorkAsync(job, phase, cancellationToken);

        logger.LogInformation(
            "MusicBrainz dump shard import finished job {JobId} phase {Phase} shard {ShardId}: imported={Imported} skipped={Skipped} lines={Lines}.",
            job.Id.Value,
            phase,
            shardId,
            imported,
            skipped,
            processed);
    }

    private async Task<MusicBrainzDumpImportJob?> TryBeginShardAsync(
        MusicBrainzDumpImportJobId jobId,
        MusicBrainzDumpImportPhase phase,
        int shardId,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < MusicBrainzDumpImportJobConcurrency.SaveAttempts; attempt++)
        {
            var job = await jobStore.GetAsync(jobId, cancellationToken);
            if (job is null)
            {
                return null;
            }

            if (job.GetOrAddShard(phase, shardId).Status == MusicBrainzDumpImportShardStatus.Completed)
            {
                return null;
            }

            if (!job.TryClaimShard(phase, shardId, leaseOwner.Value, DateTimeOffset.UtcNow, leaseDuration))
            {
                logger.LogInformation(
                    "Skipping MusicBrainz dump shard import job {JobId} phase {Phase} shard {ShardId}: not leased by this process.",
                    jobId.Value,
                    phase,
                    shardId);
                return null;
            }

            try
            {
                await jobStore.SaveAsync(job, cancellationToken);
                return job;
            }
            catch (InvalidOperationException exception) when (
                attempt < MusicBrainzDumpImportJobConcurrency.SaveAttempts - 1 &&
                MusicBrainzDumpImportJobConcurrency.IsConflict(exception))
            {
            }
        }

        logger.LogWarning(
            "Giving up MusicBrainz dump shard import job {JobId} phase {Phase} shard {ShardId} after concurrent save conflicts.",
            jobId.Value,
            phase,
            shardId);
        return null;
    }

    private async Task<MusicBrainzDumpImportJob> FlushBufferAsync(
        MusicBrainzDumpImportJob job,
        MusicBrainzDumpImportPhase phase,
        int shardId,
        List<CatalogDumpBatchItem> buffer,
        DateTimeOffset dumpObservedAt,
        long processed,
        TimeSpan leaseDuration,
        HashSet<ArtistId> touchedArtists,
        CancellationToken cancellationToken)
    {
        var appended = await batchWriter.AppendEventsAsync(buffer, dumpObservedAt, cancellationToken);
        foreach (var artistId in appended)
        {
            touchedArtists.Add(artistId);
        }

        buffer.Clear();
        return await PersistOwnedShardAsync(
            job,
            phase,
            shardId,
            processed,
            leaseDuration,
            markCompleted: false,
            cancellationToken);
    }

    private async Task<MusicBrainzDumpImportJob> PersistOwnedShardAsync(
        MusicBrainzDumpImportJob job,
        MusicBrainzDumpImportPhase phase,
        int shardId,
        long processed,
        TimeSpan leaseDuration,
        bool markCompleted,
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < MusicBrainzDumpImportJobConcurrency.SaveAttempts; attempt++)
        {
            // Always reload before mutating: parallel shard workers share one job document.
            // Saving a stale in-memory copy would pass optimistic concurrency (fresh change vector)
            // while wiping other shards' LineOffset/lease progress.
            job = await jobStore.GetAsync(job.Id, cancellationToken)
                  ?? throw new InvalidOperationException(
                      $"MusicBrainz dump job '{job.Id.Value}' disappeared during shard import.");

            var shard = job.GetOrAddShard(phase, shardId);
            if (!OwnsLease(shard) &&
                !job.TryClaimShard(phase, shardId, leaseOwner.Value, DateTimeOffset.UtcNow, leaseDuration))
            {
                throw new InvalidOperationException(
                    $"Shard '{MusicBrainzDumpImportShardState.FormatKey(phase, shardId)}' is not leased by '{leaseOwner.Value}'.");
            }

            shard = job.GetOrAddShard(phase, shardId);
            shard.UpdateLineOffset(processed);
            if (markCompleted)
            {
                shard.MarkCompleted();
            }
            else
            {
                shard.Heartbeat(leaseOwner.Value, DateTimeOffset.UtcNow, leaseDuration);
            }

            try
            {
                await jobStore.SaveAsync(job, cancellationToken);
                return job;
            }
            catch (InvalidOperationException exception) when (
                attempt < MusicBrainzDumpImportJobConcurrency.SaveAttempts - 1 &&
                MusicBrainzDumpImportJobConcurrency.IsConflict(exception))
            {
            }
        }

        throw new InvalidOperationException(
            $"Unable to persist MusicBrainz dump shard '{MusicBrainzDumpImportShardState.FormatKey(phase, shardId)}' after concurrent save conflicts.");
    }

    private async Task PersistAfterShardWorkAsync(
        MusicBrainzDumpImportJob job,
        MusicBrainzDumpImportPhase phase,
        CancellationToken cancellationToken)
    {
        var enqueueProducer = false;
        for (var attempt = 0; attempt < MusicBrainzDumpImportJobConcurrency.SaveAttempts; attempt++)
        {
            job = await jobStore.GetAsync(job.Id, cancellationToken)
                  ?? throw new InvalidOperationException(
                      $"MusicBrainz dump job '{job.Id.Value}' disappeared during shard completion.");

            enqueueProducer = false;
            if ((phase is MusicBrainzDumpImportPhase.Artists or MusicBrainzDumpImportPhase.ReleaseGroups) &&
                job.AreAllShardsCompleted(phase) &&
                job.CurrentPhase == phase &&
                job.TryAdvancePhase())
            {
                enqueueProducer = true;
            }
            else if (phase == MusicBrainzDumpImportPhase.Recordings &&
                     job.TryCompleteRecordingsPhaseAsFinal(DateTimeOffset.UtcNow))
            {
                MusicBrainzDumpImportTelemetry.MarkJobTerminal(job);
            }
            else
            {
                // Nothing to persist — another worker may already have advanced the job.
                return;
            }

            try
            {
                await jobStore.SaveAsync(job, cancellationToken);
                if (enqueueProducer)
                {
                    await downloadWorkQueue.EnqueueAsync(new DownloadDumpAndShardWork(job.Id), cancellationToken);
                }

                return;
            }
            catch (InvalidOperationException exception) when (
                attempt < MusicBrainzDumpImportJobConcurrency.SaveAttempts - 1 &&
                MusicBrainzDumpImportJobConcurrency.IsConflict(exception))
            {
            }
        }

        throw new InvalidOperationException(
            $"Unable to persist MusicBrainz dump job '{job.Id.Value}' after shard completion due to concurrent saves.");
    }

    private bool OwnsLease(MusicBrainzDumpImportShardState shard) =>
        shard.Status == MusicBrainzDumpImportShardStatus.Leased &&
        shard.Lease is { } lease &&
        string.Equals(lease.Owner, leaseOwner.Value, StringComparison.Ordinal);

    private CatalogDumpBatchItem? TryMap(MusicBrainzDumpImportPhase phase, string line) =>
        phase switch
        {
            MusicBrainzDumpImportPhase.Artists =>
                artistRowMapper.TryMap(line) is { } artist
                    ? new ArtistDumpBatchItem(artist)
                    : null,
            MusicBrainzDumpImportPhase.ReleaseGroups =>
                releaseGroupRowMapper.TryMap(line) is { } album
                    ? new AlbumDumpBatchItem(album)
                    : null,
            MusicBrainzDumpImportPhase.Recordings =>
                trackRowMapper.TryMap(line) is { } track
                    ? new TrackDumpBatchItem(track)
                    : null,
            _ => null
        };

    private static DateTimeOffset ResolveDumpObservedAt(MusicBrainzDumpImportJob job)
    {
        if (MusicBrainzDumpSnapshotId.TryGetObservedAtUtc(job.DumpVersion, out var fromVersion))
        {
            return fromVersion;
        }

        throw new InvalidOperationException(
            $"MusicBrainz dump job '{job.Id.Value}' has DumpVersion '{job.DumpVersion}' that cannot provide ObservedAt. " +
            "Use a YYYYMMDD-HHMMSS (or yyyy-MM) snapshot id.");
    }
}
