using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog.MusicBrainzDumpImport;
using Soundtrail.Domain.Catalog.MusicBrainzDumpImport.Messages;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.ImportCatalogShard.Work;
using Soundtrail.Services.Enrichment.CatalogImport.Infrastructure.Lease;

namespace Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.ImportCatalogShard;

public sealed class ImportMusicBrainzDumpShardHandler(
    IMusicBrainzDumpImportJobStore jobStore,
    IImportCatalogShardWorkQueue workQueue,
    ICatalogImportLeaseOwner leaseOwner) : IHandler<ImportMusicBrainzDumpShard>
{
    public static readonly TimeSpan ShardLeaseDuration = TimeSpan.FromMinutes(5);

    public async Task Handle(
        IncomingMessage<ImportMusicBrainzDumpShard> context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var message = context.Message;
        var job = await jobStore.GetAsync(message.JobId, cancellationToken);
        if (job is null)
        {
            return;
        }

        if (!job.TryClaimShard(
                message.Phase,
                message.ShardId,
                leaseOwner.Value,
                DateTimeOffset.UtcNow,
                ShardLeaseDuration))
        {
            return;
        }

        await jobStore.SaveAsync(job, cancellationToken);
        await workQueue.EnqueueAsync(
            new ImportCatalogShardWork(job.Id, message.Phase, message.ShardId),
            cancellationToken);
    }
}
