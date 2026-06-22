using Soundtrail.Domain;
using Soundtrail.Domain.Commands;
using Soundtrail.Domain.Model;
using Soundtrail.Domain.Responses;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.ImportMusicTrackEvents;

public sealed class ImportMusicTrackEventsHandler(
    IMusicTrackEventRepository repository) : IHandler<ImportMusicTrackEventsCommand>
{
    public async Task Handle(
        ImportMusicTrackEventsCommand command,
        CancellationToken cancellationToken = default)
    {
        await repository.AppendEventsAsync(
            command.MusicCatalogId,
            command.ExpectedVersion,
            command.CommandId,
            command.Events,
            cancellationToken);
    }
}
