using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog.MusicBrainzDumpImport;
using Soundtrail.Domain.Catalog.MusicBrainzDumpImport.Messages;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard.Adapters;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard.Model;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard.Ports;
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
        var job = await jobStore.GetAsync(jobId, cancellationToken);
        if (job is null)
        {
            return;
        }

        var leaseDuration = options.Value.LeaseDuration;
        if (!job.TryClaimProducer(leaseOwner.Value, DateTimeOffset.UtcNow, leaseDuration))
        {
            return;
        }

        await jobStore.SaveAsync(job, cancellationToken);

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

    private async Task RunArtistsPhaseAsync(
        MusicBrainzDumpImportJob job,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken)
    {
        using var activity = MusicBrainzDumpImportTelemetry.StartProducerPhaseActivity(
            job,
            MusicBrainzDumpImportPhase.Artists);

        Heartbeat(job, leaseDuration);

        var artistsPath = await archiveStore.EnsureArtistsJsonlAsync(job.Id, job.DumpVersion, cancellationToken);
        Heartbeat(job, leaseDuration);

        if (job.Status == MusicBrainzDumpImportJobStatus.Downloading)
        {
            job.SetStatus(MusicBrainzDumpImportJobStatus.Extracting);
            await jobStore.SaveAsync(job, cancellationToken);
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
                Heartbeat(job, leaseDuration);
                await jobStore.SaveAsync(job, cancellationToken);
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

        Heartbeat(job, leaseDuration);

        var releaseGroupsPath = await archiveStore.EnsureReleaseGroupsJsonlAsync(
            job.Id,
            job.DumpVersion,
            cancellationToken);
        Heartbeat(job, leaseDuration);

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
                Heartbeat(job, leaseDuration);
                await jobStore.SaveAsync(job, cancellationToken);
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

        Heartbeat(job, leaseDuration);

        var tracksPath = await archiveStore.EnsureTracksJsonlAsync(
            job.Id,
            job.DumpVersion,
            cancellationToken);
        Heartbeat(job, leaseDuration);

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
                Heartbeat(job, leaseDuration);
                await jobStore.SaveAsync(job, cancellationToken);
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

        job.RegisterPhaseShards(phase, shardCount);
        job.SetStatus(MusicBrainzDumpImportJobStatus.Importing);
        MusicBrainzDumpImportTelemetry.RecordProgress(
            job,
            MusicBrainzDumpImportProgress.AfterProducerPublished(phase));
        Heartbeat(job, leaseDuration);
        await jobStore.SaveAsync(job, cancellationToken);

        var requestedAt = DateTimeOffset.UtcNow;
        for (var shardId = 0; shardId < shardCount; shardId++)
        {
            await commandBus.SendAsync(
                ImportMusicBrainzDumpShard.Create(job.Id, phase, shardId, requestedAt),
                cancellationToken);
        }
    }

    private void Heartbeat(MusicBrainzDumpImportJob job, TimeSpan leaseDuration) =>
        job.HeartbeatProducer(leaseOwner.Value, DateTimeOffset.UtcNow, leaseDuration);
}
