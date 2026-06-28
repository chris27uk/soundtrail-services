using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Catalog.Commands;
using Soundtrail.Domain.Catalog.Events;
using Soundtrail.Contracts.Common;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnMusicTrackEventsImported;

public sealed class MusicTrackEventsImportedHandler(IEventStreamRepository<MusicCatalogId, IMusicTrackEvent> repository) : IHandler<ImportMusicTrackEventsCommand>
{
    public async Task Handle(ImportMusicTrackEventsCommand command, CancellationToken cancellationToken = default)
    {
        await repository.AppendAsync(
            new AppendRequest<MusicCatalogId, IMusicTrackEvent>(
                command.MusicCatalogId,
                command.ExpectedVersion,
                command.Events,
                OperationId.From(command.CommandId.Value)),
            cancellationToken);
    }
}
