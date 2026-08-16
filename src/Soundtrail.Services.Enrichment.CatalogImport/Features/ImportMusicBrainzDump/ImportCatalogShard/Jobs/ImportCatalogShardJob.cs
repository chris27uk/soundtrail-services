using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Soundtrail.Domain.Catalog.MusicBrainzDumpImport;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard.Model;
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

        var job = await jobStore.GetAsync(jobId, cancellationToken);
        if (job is null)
        {
            return;
        }

        using var activity = MusicBrainzDumpImportTelemetry.StartShardImportActivity(job, phase, shardId);

        var leaseDuration = options.Value.LeaseDuration;
        var shard = job.GetOrAddShard(phase, shardId);
        shard.Heartbeat(leaseOwner.Value, DateTimeOffset.UtcNow, leaseDuration);

        var dumpObservedAt = options.Value.DumpObservedAt
            ?? job.RequestedAt;
        var batchSize = Math.Max(1, options.Value.BulkInsertBatchSize);
        var processed = shard.LineOffset;
        var imported = 0;
        var skipped = 0;
        var buffer = new List<CatalogDumpBatchItem>(batchSize);

        await foreach (var line in shardStore.ReadShardLinesAsync(
                           jobId,
                           phase,
                           shardId,
                           skipLines: shard.LineOffset,
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
                await FlushBufferAsync(
                    job,
                    shard,
                    buffer,
                    dumpObservedAt,
                    processed,
                    leaseDuration,
                    cancellationToken);
            }
        }

        if (buffer.Count > 0)
        {
            await FlushBufferAsync(
                job,
                shard,
                buffer,
                dumpObservedAt,
                processed,
                leaseDuration,
                cancellationToken);
        }

        shard.UpdateLineOffset(processed);
        shard.MarkCompleted();
        MusicBrainzDumpImportTelemetry.RecordRows(job.Id.Value, imported, skipped);

        var (completedShards, totalShards) = MusicBrainzDumpImportProgress.CountPhaseShards(job, phase);
        if (totalShards > 0)
        {
            MusicBrainzDumpImportTelemetry.RecordProgress(
                job,
                MusicBrainzDumpImportProgress.AfterShardCompleted(phase, completedShards, totalShards));
        }

        if ((phase is MusicBrainzDumpImportPhase.Artists or MusicBrainzDumpImportPhase.ReleaseGroups) &&
            job.AreAllShardsCompleted(phase) &&
            job.TryAdvancePhase())
        {
            await jobStore.SaveAsync(job, cancellationToken);
            await downloadWorkQueue.EnqueueAsync(new DownloadDumpAndShardWork(job.Id), cancellationToken);
        }
        else if (phase == MusicBrainzDumpImportPhase.Recordings)
        {
            if (job.TryCompleteRecordingsPhaseAsFinal(DateTimeOffset.UtcNow))
            {
                MusicBrainzDumpImportTelemetry.MarkJobTerminal(job);
            }

            await jobStore.SaveAsync(job, cancellationToken);
        }
        else
        {
            await jobStore.SaveAsync(job, cancellationToken);
        }

        logger.LogInformation(
            "MusicBrainz dump shard import finished job {JobId} phase {Phase} shard {ShardId}: imported={Imported} skipped={Skipped} lines={Lines}.",
            job.Id.Value,
            phase,
            shardId,
            imported,
            skipped,
            processed);
    }

    private async Task FlushBufferAsync(
        MusicBrainzDumpImportJob job,
        MusicBrainzDumpImportShardState shard,
        List<CatalogDumpBatchItem> buffer,
        DateTimeOffset dumpObservedAt,
        long processed,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken)
    {
        await batchWriter.FlushAsync(buffer, dumpObservedAt, cancellationToken);
        buffer.Clear();
        shard.UpdateLineOffset(processed);
        shard.Heartbeat(leaseOwner.Value, DateTimeOffset.UtcNow, leaseDuration);
        await jobStore.SaveAsync(job, cancellationToken);
    }

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
}
