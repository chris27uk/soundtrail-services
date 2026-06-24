using Soundtrail.Domain;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Enrichment.Commands;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnStreamingLocationsRequired.Support;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnStreamingLocationsRequired;

public sealed class StreamingLocationsRequiredHandler(ICommandBus commandBus)
{
    public Task Handle(ScheduleStreamingLocationsLookupCommand command, CancellationToken cancellationToken = default)
    {
        var commandToSend = new LookupStreamingLocationsCommand(
            LookupStreamingLocationsCommand.Id(command.MusicCatalogId),
            command.MusicCatalogId,
            command.Priority,
            command.ObservedAt,
            command.CorrelationId,
            command.SearchTerm,
            new CatalogTrackHierarchy(command.ArtistId, command.AlbumId));
        return commandBus.SendAsync(commandToSend, cancellationToken);
    }
}
