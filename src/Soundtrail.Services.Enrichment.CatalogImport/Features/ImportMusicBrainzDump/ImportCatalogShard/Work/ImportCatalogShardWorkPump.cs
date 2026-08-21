using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Soundtrail.Domain.Catalog.MusicBrainzDumpImport;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.ImportCatalogShard.Ports;

namespace Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.ImportCatalogShard.Work;

public sealed class ImportCatalogShardWorkPump(
    IImportCatalogShardWorkQueue workQueue,
    IServiceScopeFactory scopeFactory,
    IOptions<MusicBrainzDumpOptions> options,
    ILogger<ImportCatalogShardWorkPump> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var parallelism = ResolveParallelism(options.Value);
        logger.LogInformation(
            "MusicBrainz dump shard import pump starting with MaxDegreeOfParallelism={MaxDegreeOfParallelism}.",
            parallelism);

        await Parallel.ForEachAsync(
            workQueue.ReadAllAsync(stoppingToken),
            new ParallelOptions
            {
                MaxDegreeOfParallelism = parallelism,
                CancellationToken = stoppingToken
            },
            async (work, cancellationToken) =>
            {
                try
                {
                    using var scope = scopeFactory.CreateScope();
                    var importCatalogShardJob = scope.ServiceProvider.GetRequiredService<IImportCatalogShardJob>();
                    await importCatalogShardJob.RunAsync(work.JobId, work.Phase, work.ShardId, cancellationToken);
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
            });
    }

    private static int ResolveParallelism(MusicBrainzDumpOptions dumpOptions)
    {
        if (dumpOptions.ShardImportMaxDegreeOfParallelism > 0)
        {
            return dumpOptions.ShardImportMaxDegreeOfParallelism;
        }

        return Math.Max(1, dumpOptions.ShardCount);
    }
}
