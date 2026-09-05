using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Soundtrail.Domain.Catalog.Albums;
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
        var projectionChunkSize = Math.Max(1, options.Value.ProjectionArtistsPerBulkInsert);
        var processed = shard.LineOffset;
        var processedBytes = shard.LineByteOffset;
        var projectionCursor = shard.ProjectionLineOffset;
        var projectionBytes = shard.ProjectionByteOffset;
        var appendStartOffset = processed;
        var imported = 0;
        var skipped = 0;
        var buffer = new List<CatalogDumpBatchItem>(batchSize);
        var artistsTouchedThisRun = new HashSet<ArtistId>();
        var projectedArtists = new HashSet<ArtistId>();

        await foreach (var row in shardStore.ReadShardLinesAsync(
                           jobId,
                           phase,
                           shardId,
                           skipLines: processed,
                           skipBytes: processedBytes,
                           cancellationToken))
        {
            processed++;
            processedBytes = row.EndByteOffset;
            var item = TryMap(phase, row.Text);
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
                (job, var touched) = await FlushBufferAsync(
                    job,
                    phase,
                    shardId,
                    buffer,
                    dumpObservedAt,
                    processed,
                    processedBytes,
                    projectionCursor,
                    projectionBytes,
                    leaseDuration,
                    cancellationToken);
                artistsTouchedThisRun.UnionWith(touched);
            }
        }

        if (buffer.Count > 0)
        {
            (job, var touched) = await FlushBufferAsync(
                job,
                phase,
                shardId,
                buffer,
                dumpObservedAt,
                processed,
                processedBytes,
                projectionCursor,
                projectionBytes,
                leaseDuration,
                cancellationToken);
            artistsTouchedThisRun.UnionWith(touched);
        }

        // Prefer projecting artists touched by this append pass (once each) instead of
        // re-scanning the shard JSONL when we appended from the projection frontier.
        job = await ProjectArtistSetAsync(
            job,
            phase,
            shardId,
            dumpObservedAt,
            processed,
            processedBytes,
            projectionCursor,
            projectionBytes,
            projectionChunkSize,
            leaseDuration,
            artistsTouchedThisRun,
            projectedArtists,
            cancellationToken);

        if (appendStartOffset == projectionCursor)
        {
            // Append started at the projection cursor: every newly written stream was projected above.
            projectionCursor = processed;
            projectionBytes = processedBytes;
            job = await PersistOwnedShardAsync(
                job,
                phase,
                shardId,
                processed,
                processedBytes,
                projectionCursor,
                projectionBytes,
                leaseDuration,
                markCompleted: false,
                cancellationToken);
        }
        else if (projectionCursor < processed)
        {
            if (phase == MusicBrainzDumpImportPhase.Recordings && artistsTouchedThisRun.Count == 0)
            {
                // Append already complete: project from Artists-phase membership (same shard partition)
                // instead of re-scanning tens of millions of Recordings JSONL lines.
                logger.LogInformation(
                    "MusicBrainz dump shard {JobId} Recordings/{ShardId}: projecting from Artists shard membership (append complete, projection {ProjectionCursor}/{Processed}).",
                    jobId.Value,
                    shardId,
                    projectionCursor,
                    processed);
                job = await ProjectFromArtistsShardMembershipAsync(
                    job,
                    jobId,
                    shardId,
                    dumpObservedAt,
                    processed,
                    processedBytes,
                    projectionCursor,
                    projectionBytes,
                    projectionChunkSize,
                    leaseDuration,
                    projectedArtists,
                    cancellationToken);
            }
            else
            {
                job = await ProjectFromShardFileAsync(
                    job,
                    jobId,
                    phase,
                    shardId,
                    dumpObservedAt,
                    processed,
                    processedBytes,
                    projectionCursor,
                    projectionBytes,
                    projectionChunkSize,
                    leaseDuration,
                    projectedArtists,
                    cancellationToken);
            }

            projectionCursor = processed;
            projectionBytes = processedBytes;
        }

        job = await PersistOwnedShardAsync(
            job,
            phase,
            shardId,
            processed,
            processedBytes,
            projectionCursor: processed,
            projectionBytes: processedBytes,
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

    private async Task<(MusicBrainzDumpImportJob Job, IReadOnlySet<ArtistId> Touched)> FlushBufferAsync(
        MusicBrainzDumpImportJob job,
        MusicBrainzDumpImportPhase phase,
        int shardId,
        List<CatalogDumpBatchItem> buffer,
        DateTimeOffset dumpObservedAt,
        long processed,
        long processedBytes,
        long projectionCursor,
        long projectionBytes,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken)
    {
        var touched = await batchWriter.AppendEventsAsync(buffer, dumpObservedAt, cancellationToken);
        buffer.Clear();
        job = await PersistOwnedShardAsync(
            job,
            phase,
            shardId,
            processed,
            processedBytes,
            projectionCursor,
            projectionBytes,
            leaseDuration,
            markCompleted: false,
            cancellationToken);
        return (job, touched);
    }

    private async Task<MusicBrainzDumpImportJob> ProjectArtistSetAsync(
        MusicBrainzDumpImportJob job,
        MusicBrainzDumpImportPhase phase,
        int shardId,
        DateTimeOffset dumpObservedAt,
        long processed,
        long processedBytes,
        long projectionCursor,
        long projectionBytes,
        int projectionChunkSize,
        TimeSpan leaseDuration,
        IReadOnlyCollection<ArtistId> artists,
        HashSet<ArtistId> projectedArtists,
        CancellationToken cancellationToken)
    {
        if (artists.Count == 0)
        {
            return job;
        }

        var pending = new HashSet<ArtistId>();
        foreach (var artistId in artists)
        {
            if (projectedArtists.Contains(artistId) || !pending.Add(artistId))
            {
                continue;
            }

            if (pending.Count < projectionChunkSize)
            {
                continue;
            }

            await batchWriter.ProjectArtistsAsync(pending, dumpObservedAt, cancellationToken);
            projectedArtists.UnionWith(pending);
            pending.Clear();
            job = await PersistOwnedShardAsync(
                job,
                phase,
                shardId,
                processed,
                processedBytes,
                projectionCursor,
                projectionBytes,
                leaseDuration,
                markCompleted: false,
                cancellationToken);
        }

        if (pending.Count > 0)
        {
            await batchWriter.ProjectArtistsAsync(pending, dumpObservedAt, cancellationToken);
            projectedArtists.UnionWith(pending);
            job = await PersistOwnedShardAsync(
                job,
                phase,
                shardId,
                processed,
                processedBytes,
                projectionCursor,
                projectionBytes,
                leaseDuration,
                markCompleted: false,
                cancellationToken);
        }

        return job;
    }

    private async Task<MusicBrainzDumpImportJob> ProjectFromArtistsShardMembershipAsync(
        MusicBrainzDumpImportJob job,
        MusicBrainzDumpImportJobId jobId,
        int shardId,
        DateTimeOffset dumpObservedAt,
        long processed,
        long processedBytes,
        long projectionCursor,
        long projectionBytes,
        int projectionChunkSize,
        TimeSpan leaseDuration,
        HashSet<ArtistId> projectedArtists,
        CancellationToken cancellationToken)
    {
        var shardCount = Math.Max(1, options.Value.ShardCount);
        var pending = new HashSet<ArtistId>();
        var seen = 0L;
        var heartbeatEvery = Math.Max(projectionChunkSize * 50L, 50_000L);

        if (options.Value.PreferRavenArtistMembershipEnumeration)
        {
            try
            {
                var ravenYielded = 0L;
                await foreach (var artistId in batchWriter.EnumerateArtistsNeedingProjectionForShardAsync(
                                   shardId,
                                   shardCount,
                                   cancellationToken))
                {
                    ravenYielded++;
                    seen++;
                    if (projectedArtists.Contains(artistId) || !pending.Add(artistId))
                    {
                        if (seen % heartbeatEvery == 0)
                        {
                            job = await PersistOwnedShardAsync(
                                job,
                                MusicBrainzDumpImportPhase.Recordings,
                                shardId,
                                processed,
                                processedBytes,
                                projectionCursor,
                                projectionBytes,
                                leaseDuration,
                                markCompleted: false,
                                cancellationToken);
                        }

                        continue;
                    }

                    if (pending.Count < projectionChunkSize)
                    {
                        continue;
                    }

                    await batchWriter.ProjectArtistsAsync(pending, dumpObservedAt, cancellationToken);
                    projectedArtists.UnionWith(pending);
                    pending.Clear();
                    job = await PersistOwnedShardAsync(
                        job,
                        MusicBrainzDumpImportPhase.Recordings,
                        shardId,
                        processed,
                        processedBytes,
                        projectionCursor,
                        projectionBytes,
                        leaseDuration,
                        markCompleted: false,
                        cancellationToken);
                }

                if (pending.Count > 0)
                {
                    await batchWriter.ProjectArtistsAsync(pending, dumpObservedAt, cancellationToken);
                    projectedArtists.UnionWith(pending);
                    pending.Clear();
                    job = await PersistOwnedShardAsync(
                        job,
                        MusicBrainzDumpImportPhase.Recordings,
                        shardId,
                        processed,
                        processedBytes,
                        projectionCursor,
                        projectionBytes,
                        leaseDuration,
                        markCompleted: false,
                        cancellationToken);
                }

                logger.LogInformation(
                    "MusicBrainz dump shard {JobId} Recordings/{ShardId}: Raven membership finished ({ArtistCount} artists needing work).",
                    jobId.Value,
                    shardId,
                    ravenYielded);
                return job;
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogWarning(
                    exception,
                    "MusicBrainz dump shard {JobId} Recordings/{ShardId}: Raven membership failed; falling back to Artists id sidecar/JSONL.",
                    jobId.Value,
                    shardId);
            }
        }

        logger.LogInformation(
            "MusicBrainz dump shard {JobId} Recordings/{ShardId}: projecting from Artists id sidecar/JSONL membership.",
            jobId.Value,
            shardId);

        // Compact .ids sidecar, or build it from Artists JSONL with peek (per-shard, ~artist-count).
        await foreach (var artistId in EnumerateArtistIdsForShardAsync(jobId, shardId, cancellationToken))
        {
            seen++;
            if (projectedArtists.Contains(artistId) || !pending.Add(artistId))
            {
                if (seen % heartbeatEvery == 0)
                {
                    job = await PersistOwnedShardAsync(
                        job,
                        MusicBrainzDumpImportPhase.Recordings,
                        shardId,
                        processed,
                        processedBytes,
                        projectionCursor,
                        projectionBytes,
                        leaseDuration,
                        markCompleted: false,
                        cancellationToken);
                }

                continue;
            }

            if (pending.Count < projectionChunkSize)
            {
                continue;
            }

            await batchWriter.ProjectArtistsAsync(pending, dumpObservedAt, cancellationToken);
            projectedArtists.UnionWith(pending);
            pending.Clear();
            job = await PersistOwnedShardAsync(
                job,
                MusicBrainzDumpImportPhase.Recordings,
                shardId,
                processed,
                processedBytes,
                projectionCursor,
                projectionBytes,
                leaseDuration,
                markCompleted: false,
                cancellationToken);
        }

        if (pending.Count > 0)
        {
            await batchWriter.ProjectArtistsAsync(pending, dumpObservedAt, cancellationToken);
            projectedArtists.UnionWith(pending);
            job = await PersistOwnedShardAsync(
                job,
                MusicBrainzDumpImportPhase.Recordings,
                shardId,
                processed,
                processedBytes,
                projectionCursor,
                projectionBytes,
                leaseDuration,
                markCompleted: false,
                cancellationToken);
        }

        return job;
    }

    private async IAsyncEnumerable<ArtistId> EnumerateArtistIdsForShardAsync(
        MusicBrainzDumpImportJobId jobId,
        int shardId,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (shardStore.ArtistIdSidecarExists(jobId, shardId))
        {
            await foreach (var id in shardStore.ReadArtistIdSidecarAsync(jobId, shardId, cancellationToken))
            {
                yield return ArtistId.From(id);
            }

            yield break;
        }

        // Lazy-build sidecar from Artists JSONL using cheap peek (one-time for this dump).
        logger.LogInformation(
            "MusicBrainz dump job {JobId} shard {ShardId}: building Artists id sidecar from JSONL.",
            jobId.Value,
            shardId);

        await using var sidecar = shardStore.OpenSingleArtistIdSidecarWriter(jobId, shardId);
        await foreach (var row in shardStore.ReadShardLinesAsync(
                           jobId,
                           MusicBrainzDumpImportPhase.Artists,
                           shardId,
                           skipLines: 0,
                           skipBytes: 0,
                           cancellationToken))
        {
            if (!artistRowMapper.TryPeekArtistId(row.Text, out var artistId))
            {
                continue;
            }

            await sidecar.AppendAsync(shardId, artistId.Value, cancellationToken);
            yield return artistId;
        }
    }

    private async Task<MusicBrainzDumpImportJob> ProjectFromShardFileAsync(
        MusicBrainzDumpImportJob job,
        MusicBrainzDumpImportJobId jobId,
        MusicBrainzDumpImportPhase phase,
        int shardId,
        DateTimeOffset dumpObservedAt,
        long processed,
        long processedBytes,
        long projectionCursor,
        long projectionBytes,
        int projectionChunkSize,
        TimeSpan leaseDuration,
        HashSet<ArtistId> projectedArtists,
        CancellationToken cancellationToken)
    {
        var pending = new HashSet<ArtistId>();
        var cursor = projectionCursor;
        var cursorBytes = projectionBytes;
        var linesSincePersist = 0L;
        var heartbeatEveryLines = Math.Max(projectionChunkSize * 50L, 50_000L);

        await foreach (var row in shardStore.ReadShardLinesAsync(
                           jobId,
                           phase,
                           shardId,
                           skipLines: projectionCursor,
                           skipBytes: projectionBytes,
                           cancellationToken))
        {
            cursor++;
            cursorBytes = row.EndByteOffset;
            linesSincePersist++;

            if (TryPeekArtistId(phase, row.Text, out var artistId) &&
                !projectedArtists.Contains(artistId) &&
                pending.Add(artistId) &&
                pending.Count >= projectionChunkSize)
            {
                await batchWriter.ProjectArtistsAsync(pending, dumpObservedAt, cancellationToken);
                projectedArtists.UnionWith(pending);
                pending.Clear();
                job = await PersistOwnedShardAsync(
                    job,
                    phase,
                    shardId,
                    processed,
                    processedBytes,
                    cursor,
                    cursorBytes,
                    leaseDuration,
                    markCompleted: false,
                    cancellationToken);
                linesSincePersist = 0;
                continue;
            }

            if (linesSincePersist >= heartbeatEveryLines)
            {
                job = await PersistOwnedShardAsync(
                    job,
                    phase,
                    shardId,
                    processed,
                    processedBytes,
                    cursor,
                    cursorBytes,
                    leaseDuration,
                    markCompleted: false,
                    cancellationToken);
                linesSincePersist = 0;
            }
        }

        if (pending.Count > 0)
        {
            await batchWriter.ProjectArtistsAsync(pending, dumpObservedAt, cancellationToken);
            projectedArtists.UnionWith(pending);
            job = await PersistOwnedShardAsync(
                job,
                phase,
                shardId,
                processed,
                processedBytes,
                cursor,
                cursorBytes,
                leaseDuration,
                markCompleted: false,
                cancellationToken);
        }

        return job;
    }

    private async Task<MusicBrainzDumpImportJob> PersistOwnedShardAsync(
        MusicBrainzDumpImportJob job,
        MusicBrainzDumpImportPhase phase,
        int shardId,
        long processed,
        long processedBytes,
        long projectionCursor,
        long projectionBytes,
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
            shard.UpdateLineOffset(processed, processedBytes);
            shard.UpdateProjectionLineOffset(projectionCursor, projectionBytes);
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

    private bool TryPeekArtistId(MusicBrainzDumpImportPhase phase, string line, out ArtistId artistId)
    {
        if (phase == MusicBrainzDumpImportPhase.Recordings &&
            trackRowMapper.TryPeekArtistId(line, out artistId))
        {
            return true;
        }

        var item = TryMap(phase, line);
        if (item is null)
        {
            artistId = default;
            return false;
        }

        artistId = ArtistIdFor(item);
        return true;
    }

    private static ArtistId ArtistIdFor(CatalogDumpBatchItem item) =>
        item switch
        {
            ArtistDumpBatchItem(var artist) => artist.Id,
            AlbumDumpBatchItem(var album) => ArtistId.From(album.AlbumId.ArtistId),
            TrackDumpBatchItem(var track) => ArtistId.From(AlbumId.From(track.AlbumId!).ArtistId),
            _ => throw new InvalidOperationException($"Unsupported dump batch item '{item.GetType().Name}'.")
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
