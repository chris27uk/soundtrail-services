using Microsoft.Extensions.Logging;
using Soundtrail.Domain.Catalog.MusicBrainzDumpImport;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.ImportCatalogShard.Ports;
using Soundtrail.Services.Enrichment.CatalogImport.Infrastructure.Lease;

namespace Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.ImportCatalogShard;

/// <summary>
/// Shard consumer stub: BulkInsert ETL lands in a later slice.
/// </summary>
public sealed class ImportCatalogShardJob(
    IMusicBrainzDumpImportJobStore jobStore,
    ICatalogImportLeaseOwner leaseOwner,
    ILogger<ImportCatalogShardJob> logger) : IImportCatalogShardJob
{
    public async Task RunAsync(
        MusicBrainzDumpImportJobId jobId,
        MusicBrainzDumpImportPhase phase,
        int shardId,
        CancellationToken cancellationToken = default)
    {
        var job = await jobStore.GetAsync(jobId, cancellationToken);
        if (job is null)
        {
            return;
        }

        var shard = job.GetOrAddShard(phase, shardId);
        shard.Heartbeat(leaseOwner.Value, DateTimeOffset.UtcNow, TimeSpan.FromMinutes(5));
        await jobStore.SaveAsync(job, cancellationToken);
        logger.LogInformation(
            "MusicBrainz dump shard import stub claimed job {JobId} phase {Phase} shard {ShardId} at line {LineOffset}.",
            job.Id.Value,
            phase,
            shardId,
            shard.LineOffset);
    }
}
