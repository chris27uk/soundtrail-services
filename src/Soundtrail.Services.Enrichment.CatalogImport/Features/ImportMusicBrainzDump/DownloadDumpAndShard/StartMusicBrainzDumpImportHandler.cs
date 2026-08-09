using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog.MusicBrainzDumpImport;
using Soundtrail.Domain.Catalog.MusicBrainzDumpImport.Messages;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard.Work;
using Soundtrail.Services.Enrichment.CatalogImport.Infrastructure.Lease;

namespace Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard;

public sealed class StartMusicBrainzDumpImportHandler(
    IMusicBrainzDumpImportJobStore jobStore,
    IDownloadDumpAndShardWorkQueue workQueue,
    ICatalogImportLeaseOwner leaseOwner) : IHandler<StartMusicBrainzDumpImport>
{
    public static readonly TimeSpan ProducerLeaseDuration = TimeSpan.FromMinutes(5);

    public async Task Handle(
        IncomingMessage<StartMusicBrainzDumpImport> context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var message = context.Message;
        var job = await jobStore.GetAsync(message.JobId, cancellationToken)
            ?? await jobStore.EnsureAsync(
                message.JobId,
                message.DumpVersion,
                message.RequestedAt,
                cancellationToken);

        if (!job.TryClaimProducer(leaseOwner.Value, DateTimeOffset.UtcNow, ProducerLeaseDuration))
        {
            return;
        }

        await jobStore.SaveAsync(job, cancellationToken);
        await workQueue.EnqueueAsync(new DownloadDumpAndShardWork(job.Id), cancellationToken);
    }
}
