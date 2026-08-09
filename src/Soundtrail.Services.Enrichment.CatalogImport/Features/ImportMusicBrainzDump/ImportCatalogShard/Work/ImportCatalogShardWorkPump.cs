using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.ImportCatalogShard.Ports;

namespace Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.ImportCatalogShard.Work;

public sealed class ImportCatalogShardWorkPump(
    IImportCatalogShardWorkQueue workQueue,
    IImportCatalogShardJob importCatalogShardJob,
    ILogger<ImportCatalogShardWorkPump> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var work in workQueue.ReadAllAsync(stoppingToken))
        {
            try
            {
                await importCatalogShardJob.RunAsync(work.JobId, work.Phase, work.ShardId, stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(
                    ex,
                    "MusicBrainz dump shard import work failed for job {JobId} phase {Phase} shard {ShardId}.",
                    work.JobId.Value,
                    work.Phase,
                    work.ShardId);
            }
        }
    }
}
