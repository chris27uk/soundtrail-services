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

        var artistsPath = await archiveStore.EnsureArtistsJsonlAsync(job.Id, job.DumpVersion, cancellationToken);
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
        var buckets = Enumerable.Range(0, shardCount).Select(_ => new List<string>()).ToArray();
        var lineCount = 0;

        await foreach (var line in File.ReadLinesAsync(artistsPath, cancellationToken))
        {
            lineCount++;
            if (!MusicBrainzArtistJsonLine.TryReadArtistId(line, out var artistId))
            {
                continue;
            }

            var shardId = partitioner.ShardIdFor(artistId, shardCount);
            buckets[shardId].Add(line);

            if (lineCount % 10_000 == 0)
            {
                job = await HeartbeatAndSaveAsync(job, leaseDuration, cancellationToken);
            }
        }

        await PublishPhaseShardsAsync(
            job,
            MusicBrainzDumpImportPhase.Artists,
            buckets,
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

        var releaseGroupsPath = await archiveStore.EnsureReleaseGroupsJsonlAsync(
            job.Id,
            job.DumpVersion,
            cancellationToken);
        job = await HeartbeatAndSaveAsync(job, leaseDuration, cancellationToken);

        var shardCount = Math.Max(1, options.Value.ShardCount);
        var buckets = Enumerable.Range(0, shardCount).Select(_ => new List<string>()).ToArray();
        var lineCount = 0;
        var copiedRows = 0;

        await foreach (var line in File.ReadLinesAsync(releaseGroupsPath, cancellationToken))
        {
            lineCount++;
            if (!MusicBrainzReleaseGroupJsonLine.TryReadCreditedArtistIds(line, out var artistIds))
            {
                continue;
            }

            foreach (var artistId in artistIds)
            {
                var shardId = partitioner.ShardIdFor(artistId, shardCount);
                buckets[shardId].Add(MusicBrainzReleaseGroupJsonLine.WrapForCreditedArtist(artistId, line));
                copiedRows++;
            }

            if (lineCount % 10_000 == 0)
            {
                job = await HeartbeatAndSaveAsync(job, leaseDuration, cancellationToken);
            }
        }

        await PublishPhaseShardsAsync(
            job,
            MusicBrainzDumpImportPhase.ReleaseGroups,
            buckets,
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

        var tracksPath = await archiveStore.EnsureTracksJsonlAsync(
            job.Id,
            job.DumpVersion,
            cancellationToken);
        job = await HeartbeatAndSaveAsync(job, leaseDuration, cancellationToken);

        var shardCount = Math.Max(1, options.Value.ShardCount);
        var buckets = Enumerable.Range(0, shardCount).Select(_ => new List<string>()).ToArray();
        var lineCount = 0;
        var copiedRows = 0;

        await foreach (var line in File.ReadLinesAsync(tracksPath, cancellationToken))
        {
            lineCount++;
            if (!MusicBrainzTrackJsonLine.TryReadCreditedArtistIds(line, out var artistIds))
            {
                continue;
            }

            foreach (var artistId in artistIds)
            {
                var shardId = partitioner.ShardIdFor(artistId, shardCount);
                buckets[shardId].Add(MusicBrainzTrackJsonLine.WrapForCreditedArtist(artistId, line));
                copiedRows++;
            }

            if (lineCount % 10_000 == 0)
            {
                job = await HeartbeatAndSaveAsync(job, leaseDuration, cancellationToken);
            }
        }

        await PublishPhaseShardsAsync(
            job,
            MusicBrainzDumpImportPhase.Recordings,
            buckets,
            leaseDuration,
            cancellationToken);

        logger.LogInformation(
            "MusicBrainz dump producer published {ShardCount} Recordings shards for job {JobId} from {LineCount} lines ({CopiedRows} credited copies).",
            shardCount,
            job.Id.Value,
            lineCount,
            copiedRows);
    }

    private async Task PublishPhaseShardsAsync(
        MusicBrainzDumpImportJob job,
        MusicBrainzDumpImportPhase phase,
        IReadOnlyList<List<string>> buckets,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken)
    {
        var shardCount = buckets.Count;
        for (var shardId = 0; shardId < shardCount; shardId++)
        {
            await shardStore.WriteShardAsync(
                job.Id,
                phase,
                shardId,
                buckets[shardId],
                cancellationToken);
        }

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
