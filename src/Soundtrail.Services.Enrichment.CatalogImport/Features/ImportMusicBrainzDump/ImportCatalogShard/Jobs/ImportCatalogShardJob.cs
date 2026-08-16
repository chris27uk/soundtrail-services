using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Soundtrail.Domain.Catalog.MusicBrainzDumpImport;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard.Model;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard.Ports;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard.Work;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.ImportCatalogShard.Ports;
using Soundtrail.Services.Enrichment.CatalogImport.Infrastructure.Lease;

namespace Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.ImportCatalogShard;

public sealed class ImportCatalogShardJob(
    IMusicBrainzDumpImportJobStore jobStore,
    ICatalogImportLeaseOwner leaseOwner,
    IMusicBrainzDumpShardStore shardStore,
    IMusicBrainzArtistDumpRowMapper artistRowMapper,
    ICatalogArtistImportWriter artistWriter,
    IMusicBrainzReleaseGroupDumpRowMapper releaseGroupRowMapper,
    ICatalogAlbumImportWriter albumWriter,
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
        if (phase is not (MusicBrainzDumpImportPhase.Artists or MusicBrainzDumpImportPhase.ReleaseGroups))
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
            var wrote = phase == MusicBrainzDumpImportPhase.Artists
                ? await TryImportArtistAsync(line, dumpObservedAt, cancellationToken)
                : await TryImportAlbumAsync(line, dumpObservedAt, cancellationToken);

            if (wrote)
            {
                imported++;
            }
            else
            {
                skipped++;
            }

            if (processed % 100 == 0)
            {
                shard.UpdateLineOffset(processed);
                shard.Heartbeat(leaseOwner.Value, DateTimeOffset.UtcNow, leaseDuration);
                await jobStore.SaveAsync(job, cancellationToken);
            }
        }

        shard.UpdateLineOffset(processed);
        shard.MarkCompleted();

        if (phase == MusicBrainzDumpImportPhase.Artists &&
            job.AreAllShardsCompleted(MusicBrainzDumpImportPhase.Artists) &&
            job.TryAdvancePhase())
        {
            await jobStore.SaveAsync(job, cancellationToken);
            await downloadWorkQueue.EnqueueAsync(new DownloadDumpAndShardWork(job.Id), cancellationToken);
        }
        else if (phase == MusicBrainzDumpImportPhase.ReleaseGroups)
        {
            job.TryCompleteReleaseGroupsPhaseAsFinal(DateTimeOffset.UtcNow);
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

    private async Task<bool> TryImportArtistAsync(
        string line,
        DateTimeOffset dumpObservedAt,
        CancellationToken cancellationToken)
    {
        var artist = artistRowMapper.TryMap(line);
        if (artist is null)
        {
            return false;
        }

        await artistWriter.WriteAsync(artist, dumpObservedAt, cancellationToken);
        return true;
    }

    private async Task<bool> TryImportAlbumAsync(
        string line,
        DateTimeOffset dumpObservedAt,
        CancellationToken cancellationToken)
    {
        var album = releaseGroupRowMapper.TryMap(line);
        if (album is null)
        {
            return false;
        }

        await albumWriter.WriteAsync(album, dumpObservedAt, cancellationToken);
        return true;
    }
}
