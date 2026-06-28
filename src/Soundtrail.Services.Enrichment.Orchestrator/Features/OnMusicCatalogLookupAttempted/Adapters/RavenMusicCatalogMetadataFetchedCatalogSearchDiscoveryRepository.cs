using Raven.Client.Documents.Session;
using Soundtrail.Adapters.EventSourcing;
using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Commands;
using Soundtrail.Domain.Search;
using Soundtrail.Adapters.Discovery;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnMusicCatalogLookupAttempted.Adapters;

public sealed class RavenMusicCatalogMetadataFetchedCatalogSearchDiscoveryRepository(
    IAsyncDocumentSession session) : ICatalogSearchDiscoveryRepository
{
    public async Task<CatalogSearchDiscoveryEventStream> LoadAsync(
        MusicSearchCriteria searchCriteria,
        CancellationToken cancellationToken)
    {
        var stream = await CreateEventStore().LoadAsync(DiscoveryQueryKey.For(searchCriteria), cancellationToken);
        return new CatalogSearchDiscoveryEventStream(stream.Version, stream.Events);
    }

    public async Task<CatalogSearchDiscoveryEventStream> LoadAsync(
        KnownCatalogItem knownItem,
        CancellationToken cancellationToken)
    {
        var stream = await CreateEventStore().LoadAsync(DiscoveryQueryKey.For(knownItem), cancellationToken);
        return new CatalogSearchDiscoveryEventStream(stream.Version, stream.Events);
    }

    public async Task<bool> AppendAsync(
        MusicSearchCriteria searchCriteria,
        int expectedVersion,
        IReadOnlyCollection<IDomainEvent> events,
        CancellationToken cancellationToken)
    {
        if (events.Count == 0)
        {
            return true;
        }

        var append = await CreateEventStore().AppendAsync(
            new AppendRequest<DiscoveryQueryKey, IDomainEvent>(
                DiscoveryQueryKey.For(searchCriteria),
                expectedVersion,
                events.ToArray()),
            cancellationToken);
        return append.Appended;
    }

    public async Task<bool> AppendAsync(
        KnownCatalogItem knownItem,
        int expectedVersion,
        IReadOnlyCollection<IDomainEvent> events,
        CancellationToken cancellationToken)
    {
        if (events.Count == 0)
        {
            return true;
        }

        var append = await CreateEventStore().AppendAsync(
            new AppendRequest<DiscoveryQueryKey, IDomainEvent>(
                DiscoveryQueryKey.For(knownItem),
                expectedVersion,
                events.ToArray()),
            cancellationToken);
        return append.Appended;
    }

    private RavenEventStore<DiscoveryQueryKey, IDomainEvent, DiscoveryQueryStoredEventRecordDto, DiscoveryQueryEventStreamMetadataRecordDto> CreateEventStore() =>
        new(
            session,
            streamId => DiscoveryQueryEventStreamMetadataRecordDto.GetDocumentId(streamId.StableValue),
            (streamId, metadataId) => new DiscoveryQueryEventStreamMetadataRecordDto
            {
                Id = metadataId,
                Criteria = streamId.StableValue
            },
            streamId => $"discovery-query-events/{streamId.StableValue}/",
            (streamId, version, _, @event) => DiscoveryQueryStoredEventTranslator.ToStoredEvent(streamId, @event, version),
            DiscoveryQueryStoredEventTranslator.ToEvent,
            storedEvent => storedEvent.OccurredAtUtc,
            storedEvent => storedEvent.Version);
}
