using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Search;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnCatalogSearchRequested.Support;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnCatalogSearchRequested;

public sealed class RecordTrackMetadataLookupRequestedHandler(
    IEventStreamRepository<DiscoveryQueryKey, IDomainEvent> discoveryRepository) : IHandler<RecordTrackMetadataLookupRequestedCommand>
{
    public async Task Handle(
        RecordTrackMetadataLookupRequestedCommand command,
        CancellationToken cancellationToken = default)
    {
        var loaded = await SearchOrSeekHistory.LoadAsync(
            discoveryRepository,
            command.SearchCriteria,
            cancellationToken);

        loaded.Aggregate.TrackMetadataLookupRequested(
            command.SearchCriteria,
            command.TrustLevel,
            command.RiskScore,
            command.OccurredAt,
            command.CorrelationId);

        await loaded.Aggregate.SaveAsync(discoveryRepository, loaded.Stream, cancellationToken);
    }
}
