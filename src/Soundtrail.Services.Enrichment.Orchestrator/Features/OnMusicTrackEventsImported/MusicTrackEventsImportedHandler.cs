using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog.Commands;
using Soundtrail.Domain.Catalog.Projection;

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
