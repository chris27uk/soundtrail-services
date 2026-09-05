using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog.MusicBrainzDumpImport;
using Soundtrail.Domain.Catalog.MusicBrainzDumpImport.Messages;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard.Adapters;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard.Ports;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard.Mapping;
using Soundtrail.Services.Enrichment.CatalogImport.Infrastructure.Lease;
using Soundtrail.Services.Enrichment.CatalogImport.Infrastructure.Telemetry;

namespace Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard;

public sealed class DownloadDumpAndShardJob(
    IMusicBrainzDumpImportJobStore jobStore,
    ICatalogImportLeaseOwner leaseOwner,
    IMusicBrainzDumpArchiveStore archiveStore,
    IMusicBrainzDumpShardStore shardStore,
    IArtistShardPartitioner partitioner,
    ICommandBus commandBus,
    IOptions<MusicBrainzDumpOptions> options,
    ILogger<DownloadDumpAndShardJob> logger) : IDownloadDumpAndShardJob
{
    public async Task RunAsync(MusicBrainzDumpImportJobId jobId, CancellationToken cancellationToken = default)
    {
        var job = await TryClaimProducerAsync(jobId, cancellationToken);
        if (job is null)
        {
            return;
        }

        var leaseDuration = options.Value.LeaseDuration;

        if (job.CurrentPhase == MusicBrainzDumpImportPhase.Artists &&
            !job.HasRegisteredShards(MusicBrainzDumpImportPhase.Artists))
        {
            await RunArtistsPhaseAsync(job, leaseDuration, cancellationToken);
            return;
        }

        if (job.CurrentPhase == MusicBrainzDumpImportPhase.ReleaseGroups &&
            !job.HasRegisteredShards(MusicBrainzDumpImportPhase.ReleaseGroups))
        {
            await RunReleaseGroupsPhaseAsync(job, leaseDuration, cancellationToken);
            return;
        }

        if (job.CurrentPhase == MusicBrainzDumpImportPhase.Recordings &&
            !job.HasRegisteredShards(MusicBrainzDumpImportPhase.Recordings))
        {
            await RunRecordingsPhaseAsync(job, leaseDuration, cancellationToken);
        }
    }

    private async Task<MusicBrainzDumpImportJob?> TryClaimProducerAsync(
        MusicBrainzDumpImportJobId jobId,
        CancellationToken cancellationToken)
    {
        var leaseDuration = options.Value.LeaseDuration;
        for (var attempt = 0; attempt < MusicBrainzDumpImportJobConcurrency.SaveAttempts; attempt++)
        {
            var job = await jobStore.GetAsync(jobId, cancellationToken);
            if (job is null)
            {
                return null;
            }

            if (!job.TryClaimProducer(leaseOwner.Value, DateTimeOffset.UtcNow, leaseDuration))
            {
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

        return null;
    }

    private async Task RunArtistsPhaseAsync(
        MusicBrainzDumpImportJob job,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken)
    {
        using var activity = MusicBrainzDumpImportTelemetry.StartProducerPhaseActivity(
            job,
            MusicBrainzDumpImportPhase.Artists);

        job = await HeartbeatAndSaveAsync(job, leaseDuration, cancellationToken);

        if (job.Status == MusicBrainzDumpImportJobStatus.Downloading)
        {
            job = await PersistProducerAsync(
                job,
                leaseDuration,
                static candidate =>
                {
                    if (candidate.Status == MusicBrainzDumpImportJobStatus.Downloading)
                    {
                        candidate.SetStatus(MusicBrainzDumpImportJobStatus.Extracting);
                    }

                    return true;
                },
                cancellationToken);
        }

        var shardCount = Math.Max(1, options.Value.ShardCount);
        var lineCount = 0;
        var jobHolder = job;
        using var heartbeatCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        using var heartbeatGate = new SemaphoreSlim(1, 1);
        var heartbeatTask = HeartbeatProducerUntilCancelledAsync(
            () => jobHolder,
            updated => jobHolder = updated,
            heartbeatGate,
            leaseDuration,
            heartbeatCts.Token);

        try
        {
            await using (var writer = shardStore.OpenWriter(job.Id, MusicBrainzDumpImportPhase.Artists, shardCount))
            await using (var idSidecar = shardStore.OpenArtistIdSidecarWriter(job.Id, shardCount))
            {
                await foreach (var line in archiveStore.ReadArtistLinesAsync(
                                   job.Id,
                                   job.DumpVersion,
                                   cancellationToken))
                {
                    lineCount++;
                    if (!MusicBrainzArtistJsonLine.TryReadArtistId(line, out var artistId))
                    {
                        continue;
                    }

                    var shardId = partitioner.ShardIdFor(artistId, shardCount);
                    await writer.AppendAsync(shardId, line, cancellationToken);
                    await idSidecar.AppendAsync(shardId, artistId, cancellationToken);

                    if (lineCount % 10_000 == 0)
                    {
                        await heartbeatGate.WaitAsync(cancellationToken);
                        try
                        {
                            jobHolder = await HeartbeatAndSaveAsync(
                                jobHolder,
                                leaseDuration,
                                cancellationToken);
                        }
                        finally
                        {
                            heartbeatGate.Release();
                        }
                    }
                }

                await writer.CompleteAsync(cancellationToken);
            }
        }
        finally
        {
            await heartbeatCts.CancelAsync();
            try
            {
                await heartbeatTask;
            }
            catch (OperationCanceledException)
            {
            }
        }

        job = jobHolder;

        await PublishPhaseShardsAsync(
            job,
            MusicBrainzDumpImportPhase.Artists,
            shardCount,
            leaseDuration,
            cancellationToken);

        logger.LogInformation(
            "MusicBrainz dump producer published {ShardCount} Artists shards for job {JobId} from {LineCount} lines.",
            shardCount,
            job.Id.Value,
            lineCount);
    }

    private async Task RunReleaseGroupsPhaseAsync(
        MusicBrainzDumpImportJob job,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken)
    {
        using var activity = MusicBrainzDumpImportTelemetry.StartProducerPhaseActivity(
            job,
            MusicBrainzDumpImportPhase.ReleaseGroups);

        job = await HeartbeatAndSaveAsync(job, leaseDuration, cancellationToken);

        var shardCount = Math.Max(1, options.Value.ShardCount);
        var lineCount = 0;
        var copiedRows = 0;
        var jobHolder = job;
        using var heartbeatCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        using var heartbeatGate = new SemaphoreSlim(1, 1);
        var heartbeatTask = HeartbeatProducerUntilCancelledAsync(
            () => jobHolder,
            updated => jobHolder = updated,
            heartbeatGate,
            leaseDuration,
            heartbeatCts.Token);

        try
        {
            await using (var writer = shardStore.OpenWriter(
                             job.Id,
                             MusicBrainzDumpImportPhase.ReleaseGroups,
                             shardCount))
            {
                await foreach (var line in archiveStore.ReadReleaseGroupLinesAsync(
                                   job.Id,
                                   job.DumpVersion,
                                   cancellationToken))
                {
                    lineCount++;
                    if (!MusicBrainzReleaseGroupJsonLine.TryReadCreditedArtistIds(line, out var artistIds))
                    {
                        continue;
                    }

                    foreach (var artistId in artistIds)
                    {
                        await writer.AppendAsync(
                            partitioner.ShardIdFor(artistId, shardCount),
                            MusicBrainzReleaseGroupJsonLine.WrapForCreditedArtist(artistId, line),
                            cancellationToken);
                        copiedRows++;
                    }

                    if (lineCount % 10_000 == 0)
                    {
                        await heartbeatGate.WaitAsync(cancellationToken);
                        try
                        {
                            jobHolder = await HeartbeatAndSaveAsync(
                                jobHolder,
                                leaseDuration,
                                cancellationToken);
                        }
                        finally
                        {
                            heartbeatGate.Release();
                        }
                    }
                }

                await writer.CompleteAsync(cancellationToken);
            }
        }
        finally
        {
            await heartbeatCts.CancelAsync();
            try
            {
                await heartbeatTask;
            }
            catch (OperationCanceledException)
            {
            }
        }

        job = jobHolder;

        await PublishPhaseShardsAsync(
            job,
            MusicBrainzDumpImportPhase.ReleaseGroups,
            shardCount,
            leaseDuration,
            cancellationToken);

        logger.LogInformation(
            "MusicBrainz dump producer published {ShardCount} ReleaseGroups shards for job {JobId} from {LineCount} lines ({CopiedRows} credited copies).",
            shardCount,
            job.Id.Value,
            lineCount,
            copiedRows);
    }

    private async Task RunRecordingsPhaseAsync(
        MusicBrainzDumpImportJob job,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken)
    {
        using var activity = MusicBrainzDumpImportTelemetry.StartProducerPhaseActivity(
            job,
            MusicBrainzDumpImportPhase.Recordings);

        job = await HeartbeatAndSaveAsync(job, leaseDuration, cancellationToken);

        var shardCount = Math.Max(1, options.Value.ShardCount);
        var joinDop = options.Value.RecordingsJoinMaxDegreeOfParallelism > 0
            ? options.Value.RecordingsJoinMaxDegreeOfParallelism
            : Math.Max(1, Environment.ProcessorCount);
        var skipLines = job.RecordingsProducerInputLinesCompleted;
        var append = skipLines > 0;
        var startedAt = DateTimeOffset.UtcNow;
        var jobHolder = job;
        long latestInputLines = skipLines;
        long latestTrackRows = 0;
        long latestCopiedRows = 0;
        using var heartbeatCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        using var heartbeatGate = new SemaphoreSlim(1, 1);
        var heartbeatTask = HeartbeatProducerUntilCancelledAsync(
            () => jobHolder,
            updated => jobHolder = updated,
            heartbeatGate,
            leaseDuration,
            () => latestInputLines,
            heartbeatCts.Token);

        logger.LogInformation(
            "MusicBrainz dump Recordings producer starting for job {JobId} (skipInputLines={SkipLines}, append={Append}, joinDop={JoinDop}).",
            job.Id.Value,
            skipLines,
            append,
            joinDop);

        try
        {
            await using (var writer = shardStore.OpenWriter(
                             job.Id,
                             MusicBrainzDumpImportPhase.Recordings,
                             shardCount,
                             append))
            {
                ValueTask OnProgressAsync(
                    MusicBrainzRecordingsShardPipeline.Progress progress,
                    CancellationToken ct)
                {
                    latestInputLines = progress.InputLinesCompleted;
                    latestTrackRows = progress.TrackRows;
                    latestCopiedRows = progress.CopiedRows;
                    logger.LogInformation(
                        "MusicBrainz dump Recordings producer progress job {JobId}: inputLines={InputLines} trackRows={TrackRows} copiedRows={CopiedRows} elapsed={Elapsed}.",
                        job.Id.Value,
                        progress.InputLinesCompleted,
                        progress.TrackRows,
                        progress.CopiedRows,
                        DateTimeOffset.UtcNow - startedAt);
                    return ValueTask.CompletedTask;
                }

                MusicBrainzRecordingsShardPipeline.Progress progress;
                if (archiveStore.HasCachedDenormalizedTrackSource(job.DumpVersion))
                {
                    progress = await MusicBrainzRecordingsShardPipeline.ProcessTrackLinesAsync(
                        archiveStore.ReadDenormalizedTrackLinesAsync(
                            job.Id,
                            job.DumpVersion,
                            cancellationToken),
                        skipLines,
                        shardCount,
                        partitioner,
                        writer,
                        OnProgressAsync,
                        cancellationToken);
                }
                else
                {
                    progress = await MusicBrainzRecordingsShardPipeline.ProcessReleaseGraphAsync(
                        archiveStore.ReadReleaseLinesAsync(
                            job.Id,
                            job.DumpVersion,
                            cancellationToken),
                        skipLines,
                        joinDop,
                        shardCount,
                        partitioner,
                        writer,
                        OnProgressAsync,
                        cancellationToken);
                }

                latestInputLines = progress.InputLinesCompleted;
                latestTrackRows = progress.TrackRows;
                latestCopiedRows = progress.CopiedRows;
                await writer.CompleteAsync(cancellationToken);
            }
        }
        finally
        {
            await heartbeatCts.CancelAsync();
            try
            {
                await heartbeatTask;
            }
            catch (OperationCanceledException)
            {
            }
        }

        job = jobHolder;
        job = await PersistProducerAsync(
            job,
            leaseDuration,
            static candidate =>
            {
                candidate.ClearRecordingsProducerCheckpoint();
                return true;
            },
            cancellationToken);

        await PublishPhaseShardsAsync(
            job,
            MusicBrainzDumpImportPhase.Recordings,
            shardCount,
            leaseDuration,
            cancellationToken);

        logger.LogInformation(
            "MusicBrainz dump producer published {ShardCount} Recordings shards for job {JobId} from {LineCount} input lines ({TrackRows} tracks, {CopiedRows} credited copies) in {Elapsed}.",
            shardCount,
            job.Id.Value,
            latestInputLines,
            latestTrackRows,
            latestCopiedRows,
            DateTimeOffset.UtcNow - startedAt);
    }

    private async Task PublishPhaseShardsAsync(
        MusicBrainzDumpImportJob job,
        MusicBrainzDumpImportPhase phase,
        int shardCount,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken)
    {
        job = await PersistProducerAsync(
            job,
            leaseDuration,
            candidate =>
            {
                if (candidate.HasRegisteredShards(phase))
                {
                    return true;
                }

                candidate.RegisterPhaseShards(phase, shardCount);
                candidate.SetStatus(MusicBrainzDumpImportJobStatus.Importing);
                MusicBrainzDumpImportTelemetry.RecordProgress(
                    candidate,
                    MusicBrainzDumpImportProgress.AfterProducerPublished(phase));
                return true;
            },
            cancellationToken);

        var requestedAt = DateTimeOffset.UtcNow;
        for (var shardId = 0; shardId < shardCount; shardId++)
        {
            await commandBus.SendAsync(
                ImportMusicBrainzDumpShard.Create(job.Id, phase, shardId, requestedAt),
                cancellationToken);
        }
    }

    private async Task HeartbeatProducerUntilCancelledAsync(
        Func<MusicBrainzDumpImportJob> getJob,
        Action<MusicBrainzDumpImportJob> setJob,
        SemaphoreSlim gate,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken) =>
        await HeartbeatProducerUntilCancelledAsync(
            getJob,
            setJob,
            gate,
            leaseDuration,
            checkpointLines: null,
            cancellationToken);

    private async Task HeartbeatProducerUntilCancelledAsync(
        Func<MusicBrainzDumpImportJob> getJob,
        Action<MusicBrainzDumpImportJob> setJob,
        SemaphoreSlim gate,
        TimeSpan leaseDuration,
        Func<long>? checkpointLines,
        CancellationToken cancellationToken)
    {
        var intervalTicks = Math.Max(TimeSpan.FromSeconds(15).Ticks, leaseDuration.Ticks / 3);
        using var timer = new PeriodicTimer(TimeSpan.FromTicks(intervalTicks));
        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                await gate.WaitAsync(cancellationToken);
                try
                {
                    setJob(
                        await PersistProducerAsync(
                            getJob(),
                            leaseDuration,
                            candidate =>
                            {
                                if (checkpointLines is not null)
                                {
                                    candidate.SetRecordingsProducerInputLinesCompleted(checkpointLines());
                                }

                                return true;
                            },
                            cancellationToken));
                }
                finally
                {
                    gate.Release();
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
    }

    private async Task<MusicBrainzDumpImportJob> HeartbeatAndSaveAsync(
        MusicBrainzDumpImportJob job,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken) =>
        await PersistProducerAsync(job, leaseDuration, _ => true, cancellationToken);

    private async Task<MusicBrainzDumpImportJob> PersistProducerAsync(
        MusicBrainzDumpImportJob job,
        TimeSpan leaseDuration,
        Func<MusicBrainzDumpImportJob, bool> apply,
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < MusicBrainzDumpImportJobConcurrency.SaveAttempts; attempt++)
        {
            if (!job.TryClaimProducer(leaseOwner.Value, DateTimeOffset.UtcNow, leaseDuration) &&
                !OwnsProducerLease(job))
            {
                throw new InvalidOperationException(
                    $"Producer lease for MusicBrainz dump job '{job.Id.Value}' is not held by '{leaseOwner.Value}'.");
            }

            Heartbeat(job, leaseDuration);
            if (!apply(job))
            {
                return job;
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
                job = await jobStore.GetAsync(job.Id, cancellationToken)
                      ?? throw new InvalidOperationException(
                          $"MusicBrainz dump job '{job.Id.Value}' disappeared during producer save.");
            }
        }

        throw new InvalidOperationException(
            $"Unable to save MusicBrainz dump producer state for job '{job.Id.Value}' after concurrent save conflicts.");
    }

    private bool OwnsProducerLease(MusicBrainzDumpImportJob job) =>
        job.ProducerLease is { } lease &&
        string.Equals(lease.Owner, leaseOwner.Value, StringComparison.Ordinal);

    private void Heartbeat(MusicBrainzDumpImportJob job, TimeSpan leaseDuration) =>
        job.HeartbeatProducer(leaseOwner.Value, DateTimeOffset.UtcNow, leaseDuration);
}
