using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Wolverine;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.SchedulePlaybackReferencesLookup;

public sealed class SchedulePlaybackReferencesLookupHandler(IMessageBus messageBus)
{
    public Task Handle(
        SchedulePlaybackReferencesLookupCommand command,
        CancellationToken cancellationToken = default)
    {
        return messageBus.SendAsync(
            new ResolvePlaybackReferencesCommandDto(
                CommandId.For($"ResolvePlaybackReferences:{command.MusicCatalogId.Value}").Value,
                command.MusicCatalogId.Value,
                command.Priority,
                command.ObservedAt,
                command.CorrelationId.Value,
                command.SearchTerm,
                command.ArtistId,
                command.AlbumId)).AsTask();
    }
}
