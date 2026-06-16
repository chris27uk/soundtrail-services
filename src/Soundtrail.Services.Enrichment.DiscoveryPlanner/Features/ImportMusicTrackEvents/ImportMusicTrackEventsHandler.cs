using Soundtrail.Domain;
using Soundtrail.Domain.Commands;
using Soundtrail.Domain.Model;
using Soundtrail.Domain.Responses;

namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.ImportMusicTrackEvents;

public sealed class ImportMusicTrackEventsHandler(
    IMusicTrackEventRepository repository) : IHandler<ImportMusicTrackEventsCommand, ImportMusicTrackEventsResult>
{
    public async Task<ImportMusicTrackEventsResult> Handle(
        ImportMusicTrackEventsCommand command,
        CancellationToken cancellationToken = default)
    {
        var append = await repository.AppendEventsAsync(
            command.MusicCatalogId,
            command.ExpectedVersion,
            command.CommandId,
            command.Events,
            cancellationToken);

        return new ImportMusicTrackEventsResult(
            append.Appended,
            append.Appended ? append.AppendedEvents.Count : 0);
    }
}
