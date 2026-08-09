using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard.Ports;

namespace Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard.Work;

public sealed class DownloadDumpAndShardWorkPump(
    IDownloadDumpAndShardWorkQueue workQueue,
    IDownloadDumpAndShardJob downloadDumpAndShardJob,
    ILogger<DownloadDumpAndShardWorkPump> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var work in workQueue.ReadAllAsync(stoppingToken))
        {
            try
            {
                await downloadDumpAndShardJob.RunAsync(work.JobId, stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(
                    ex,
                    "MusicBrainz dump download/shard work failed for job {JobId}.",
                    work.JobId.Value);
            }
        }
    }
}
