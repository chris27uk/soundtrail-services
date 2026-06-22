using Raven.Client.Documents;
using Raven.Client.Exceptions;
using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Domain.Discovery;
using Soundtrail.Services.Enrichment.Orchestrator.Features.JustInTimeScheduling.Adapters.Mappers;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.JustInTimeScheduling.Adapters;

public sealed class RavenCatalogSearchDiscoveryRepository(IDocumentStore documentStore) : ICatalogSearchDiscoveryRepository
{
    public async Task<CatalogSearchDiscoveryEventStream> LoadAsync(
        CatalogSearchCriteria criteria,
        CancellationToken cancellationToken)
    {
        using var session = documentStore.OpenAsyncSession();
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

        using var session = documentStore.OpenAsyncSession();
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

        try
        {
            await session.SaveChangesAsync(cancellationToken);
        }
        catch (ConcurrencyException)
        {
            return false;
        }
        return true;
    }
}
