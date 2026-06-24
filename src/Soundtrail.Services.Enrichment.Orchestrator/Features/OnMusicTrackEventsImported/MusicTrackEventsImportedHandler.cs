using Soundtrail.Domain;
using Soundtrail.Domain.Commands;
using Soundtrail.Domain.Model;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnMusicTrackEventsImported;

public sealed class MusicTrackEventsImportedHandler(IMusicTrackEventRepository repository) : IHandler<ImportMusicTrackEventsCommand>
{
    public async Task Handle(ImportMusicTrackEventsCommand command, CancellationToken cancellationToken = default)
    {
        await repository.AppendEventsAsync(
            command.MusicCatalogId,
            command.ExpectedVersion,
            command.CommandId,
            command.Events,
            cancellationToken);
    }
}
