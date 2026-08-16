using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog.MusicBrainzDumpImport;
using Soundtrail.Domain.Catalog.MusicBrainzDumpImport.Messages;
using Soundtrail.Domain.Operations;
using Soundtrail.Services.Enrichment.Scheduler.Features.ImportMusicBrainzDump.Ports;

namespace Soundtrail.Services.Enrichment.Scheduler.Features.ImportMusicBrainzDump;

public sealed class ImportMusicBrainzDumpHandler(
    IMusicBrainzDumpImportJobStore jobStore,
    ICommandBus commandBus,
    IMusicBrainzDumpSnapshotCatalog snapshotCatalog) : IScheduledMessageHandler<ImportMusicBrainzDumpCommand>
{
    public async Task HandleAsync(
        ImportMusicBrainzDumpCommand request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        MusicBrainzDumpSnapshotId snapshotId;
        if (request.Manual)
        {
            if (request.SnapshotId is null)
            {
                throw new InvalidOperationException(
                    "Manual MusicBrainz dump import requires a concrete DumpVersion snapshot id.");
            }

            snapshotId = request.SnapshotId.Value;
            if (!await snapshotCatalog.SnapshotExistsAsync(snapshotId, cancellationToken))
            {
                throw new InvalidOperationException(
                    $"MusicBrainz dump snapshot '{snapshotId.Value}' was not found at the configured BaseUrl.");
            }
        }
        else
        {
            snapshotId = await snapshotCatalog.GetLatestSnapshotIdAsync(cancellationToken);
        }

        var jobId = MusicBrainzDumpImportJobId.ForSnapshot(snapshotId);
        var job = await jobStore.EnsureAsync(jobId, snapshotId.Value, request.TriggeredAt, cancellationToken);

        await commandBus.SendAsync(
            StartMusicBrainzDumpImport.Create(job.Id, job.DumpVersion, request.TriggeredAt),
            cancellationToken);
    }
}
