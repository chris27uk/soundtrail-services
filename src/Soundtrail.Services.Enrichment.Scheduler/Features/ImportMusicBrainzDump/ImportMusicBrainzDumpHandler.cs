using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog.MusicBrainzDumpImport;
using Soundtrail.Domain.Catalog.MusicBrainzDumpImport.Messages;
using Soundtrail.Domain.Operations;

namespace Soundtrail.Services.Enrichment.Scheduler.Features.ImportMusicBrainzDump;

public sealed class ImportMusicBrainzDumpHandler(
    IMusicBrainzDumpImportJobStore jobStore,
    ICommandBus commandBus) : IScheduledMessageHandler<ImportMusicBrainzDumpCommand>
{
    public async Task HandleAsync(
        ImportMusicBrainzDumpCommand request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var dumpVersion = FormatDumpVersion(request.TriggeredAt);
        var jobId = MusicBrainzDumpImportJobId.ForDumpVersion(dumpVersion);
        var job = await jobStore.EnsureAsync(jobId, dumpVersion, request.TriggeredAt, cancellationToken);

        await commandBus.SendAsync(
            StartMusicBrainzDumpImport.Create(job.Id, job.DumpVersion, request.TriggeredAt),
            cancellationToken);
    }

    private static string FormatDumpVersion(DateTimeOffset triggeredAt)
    {
        var utc = triggeredAt.ToUniversalTime();
        return $"{utc.Year:D4}-{utc.Month:D2}";
    }
}
