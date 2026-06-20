using Raven.Client.Documents;
using Raven.Client.Documents.Session;
using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Domain.Discovery;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.EnrichmentResponse.Support;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.JustInTimeScheduling.Adapters.Mappers;

namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.EnrichmentResponse.Adapters;

public sealed class RavenEnrichmentResponseCatalogSearchDiscoveryRepository(
    IAsyncDocumentSession session) : ICatalogSearchDiscoveryRepository,
    ICompleteTrackedDiscoveriesRepository
{
    public async Task<CatalogSearchDiscoveryEventStream> LoadAsync(
        CatalogSearchCriteria criteria,
        CancellationToken cancellationToken)
    {
        var metadata = await session.LoadAsync<DiscoveryQueryEventStreamMetadataRecordDto>(
            DiscoveryQueryEventStreamMetadataRecordDto.GetDocumentId(criteria.Value),
            cancellationToken);

        if (metadata is null)
        {
            return new CatalogSearchDiscoveryEventStream(0, []);
        }

        var storedEvents = (await session.Advanced.LoadStartingWithAsync<DiscoveryQueryStoredEventRecordDto>(
                $"discovery-query-events/{criteria.Value}/"))
            .OrderBy(x => x.Version)
            .ToList();

        return new CatalogSearchDiscoveryEventStream(
            metadata.Version,
            storedEvents.Select(CatalogSearchDiscoveryEventRecordMapper.ToDomainEvent).ToArray());
    }

    public async Task<bool> AppendAsync(
        CatalogSearchCriteria criteria,
        int expectedVersion,
        IReadOnlyCollection<Soundtrail.Domain.Events.IDomainEvent> events,
        CancellationToken cancellationToken)
    {
        if (events.Count == 0)
        {
            return true;
        }

        session.Advanced.UseOptimisticConcurrency = true;

        var metadataId = DiscoveryQueryEventStreamMetadataRecordDto.GetDocumentId(criteria.Value);
        var metadata = await session.LoadAsync<DiscoveryQueryEventStreamMetadataRecordDto>(metadataId, cancellationToken)
            ?? new DiscoveryQueryEventStreamMetadataRecordDto
            {
                Id = metadataId,
                Criteria = criteria.Value
            };

        if (metadata.Version != expectedVersion)
        {
            return false;
        }

        var startingVersion = metadata.Version;
        metadata.Version += events.Count;
        metadata.UpdatedAtUtc = events.Max(CatalogSearchDiscoveryEventRecordMapper.GetOccurredAtUtc);

        await session.StoreAsync(metadata, cancellationToken);

        foreach (var storedEvent in CatalogSearchDiscoveryEventRecordMapper.ToStoredEvents(criteria, events, startingVersion))
        {
            await session.StoreAsync(storedEvent, cancellationToken);
        }

        return true;
    }
}
