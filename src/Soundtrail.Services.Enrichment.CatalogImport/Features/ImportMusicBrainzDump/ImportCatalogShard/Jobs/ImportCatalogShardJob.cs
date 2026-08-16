using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Soundtrail.Domain.Catalog.MusicBrainzDumpImport;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard.Model;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard.Ports;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.ImportCatalogShard.Ports;
using Soundtrail.Services.Enrichment.CatalogImport.Infrastructure.Lease;

namespace Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.ImportCatalogShard;

public sealed class ImportCatalogShardJob(
    IMusicBrainzDumpImportJobStore jobStore,
    ICatalogImportLeaseOwner leaseOwner,
    IMusicBrainzDumpShardStore shardStore,
    IMusicBrainzArtistDumpRowMapper rowMapper,
    ICatalogArtistImportWriter artistWriter,
    IOptions<MusicBrainzDumpOptions> options,
    ILogger<ImportCatalogShardJob> logger) : IImportCatalogShardJob
{
    public async Task RunAsync(
        MusicBrainzDumpImportJobId jobId,
        MusicBrainzDumpImportPhase phase,
        int shardId,
        CancellationToken cancellationToken = default)
    {
        if (phase != MusicBrainzDumpImportPhase.Artists)
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

        var leaseDuration = options.Value.LeaseDuration;
        var shard = job.GetOrAddShard(phase, shardId);
        shard.Heartbeat(leaseOwner.Value, DateTimeOffset.UtcNow, leaseDuration);

        var dumpObservedAt = options.Value.DumpObservedAt
            ?? job.RequestedAt;
        var processed = shard.LineOffset;
        var imported = 0;
        var skipped = 0;

        await foreach (var line in shardStore.ReadShardLinesAsync(
                           jobId,
                           phase,
                           shardId,
                           skipLines: shard.LineOffset,
                           cancellationToken))
        {
            processed++;
            var artist = rowMapper.TryMap(line);
            if (artist is null)
            {
                skipped++;
                continue;
            }

            await artistWriter.WriteAsync(artist, dumpObservedAt, cancellationToken);
            imported++;

            if (processed % 100 == 0)
            {
                shard.UpdateLineOffset(processed);
                shard.Heartbeat(leaseOwner.Value, DateTimeOffset.UtcNow, leaseDuration);
                await jobStore.SaveAsync(job, cancellationToken);
            }
        }

        shard.UpdateLineOffset(processed);
        shard.MarkCompleted();
        job.TryCompleteArtistsPhaseAsFinal(DateTimeOffset.UtcNow);
        await jobStore.SaveAsync(job, cancellationToken);

        logger.LogInformation(
            "MusicBrainz dump shard import finished job {JobId} phase {Phase} shard {ShardId}: imported={Imported} skipped={Skipped} lines={Lines}.",
            job.Id.Value,
            phase,
            shardId,
            imported,
            skipped,
            processed);
    }
}
