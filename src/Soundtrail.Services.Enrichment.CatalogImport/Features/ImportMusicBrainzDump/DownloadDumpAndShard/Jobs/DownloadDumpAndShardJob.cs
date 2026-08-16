using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog.MusicBrainzDumpImport;
using Soundtrail.Domain.Catalog.MusicBrainzDumpImport.Messages;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard.Adapters;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard.Model;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard.Ports;
using Soundtrail.Services.Enrichment.CatalogImport.Infrastructure.Lease;

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
        Heartbeat(job, leaseDuration);

        var artistsPath = await archiveStore.EnsureArtistsJsonlAsync(jobId, job.DumpVersion, cancellationToken);
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

        for (var shardId = 0; shardId < shardCount; shardId++)
        {
            await shardStore.WriteShardAsync(
                jobId,
                MusicBrainzDumpImportPhase.Artists,
                shardId,
                buckets[shardId],
                cancellationToken);
        }

        job.RegisterPhaseShards(MusicBrainzDumpImportPhase.Artists, shardCount);
        job.SetStatus(MusicBrainzDumpImportJobStatus.Importing);
        Heartbeat(job, leaseDuration);
        await jobStore.SaveAsync(job, cancellationToken);

        var requestedAt = DateTimeOffset.UtcNow;
        for (var shardId = 0; shardId < shardCount; shardId++)
        {
            await commandBus.SendAsync(
                ImportMusicBrainzDumpShard.Create(
                    jobId,
                    MusicBrainzDumpImportPhase.Artists,
                    shardId,
                    requestedAt),
                cancellationToken);
        }

        logger.LogInformation(
            "MusicBrainz dump producer published {ShardCount} Artists shards for job {JobId} from {LineCount} lines.",
            shardCount,
            job.Id.Value,
            lineCount);
    }

    private void Heartbeat(MusicBrainzDumpImportJob job, TimeSpan leaseDuration) =>
        job.HeartbeatProducer(leaseOwner.Value, DateTimeOffset.UtcNow, leaseDuration);
}
