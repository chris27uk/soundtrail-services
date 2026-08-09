using Microsoft.Extensions.Logging;
using Soundtrail.Domain.Catalog.MusicBrainzDumpImport;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard.Ports;
using Soundtrail.Services.Enrichment.CatalogImport.Infrastructure.Lease;

namespace Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard;

/// <summary>
/// Producer stub: download/decompress/shard ETL lands in a later slice.
/// </summary>
public sealed class DownloadDumpAndShardJob(
    IMusicBrainzDumpImportJobStore jobStore,
    ICatalogImportLeaseOwner leaseOwner,
    ILogger<DownloadDumpAndShardJob> logger) : IDownloadDumpAndShardJob
{
    public async Task RunAsync(MusicBrainzDumpImportJobId jobId, CancellationToken cancellationToken = default)
    {
        var job = await jobStore.GetAsync(jobId, cancellationToken);
        if (job is null)
        {
            return;
        }

        job.HeartbeatProducer(
            leaseOwner.Value,
            DateTimeOffset.UtcNow,
            TimeSpan.FromMinutes(5));

        if (job.Status == MusicBrainzDumpImportJobStatus.Downloading)
        {
            job.SetStatus(MusicBrainzDumpImportJobStatus.Extracting);
        }

        await jobStore.SaveAsync(job, cancellationToken);
        logger.LogInformation(
            "MusicBrainz dump download/shard stub claimed job {JobId} at status {Status}.",
            job.Id.Value,
            job.Status);
    }
}
