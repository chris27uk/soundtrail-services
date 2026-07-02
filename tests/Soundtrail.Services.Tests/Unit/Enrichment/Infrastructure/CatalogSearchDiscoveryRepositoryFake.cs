using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Commands;
using Soundtrail.Domain.Search;

namespace Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

internal sealed class CatalogSearchDiscoveryRepositoryFake :
    IEventStreamRepository<DiscoveryQueryKey, IDomainEvent>
{
    private readonly Dictionary<string, List<IDomainEvent>> eventsByCriteria = [];

    public void Seed(
        MusicSearchCriteria searchCriteria,
        params IDomainEvent[] events)
    {
        if (!eventsByCriteria.TryGetValue(ToPersistentId(searchCriteria), out var stored))
        {
            stored = [];
            eventsByCriteria[ToPersistentId(searchCriteria)] = stored;
        }

        stored.AddRange(events);
    }

    public IReadOnlyList<IDomainEvent> GetStoredEvents(MusicSearchCriteria searchCriteria) =>
        eventsByCriteria.TryGetValue(ToPersistentId(searchCriteria), out var events)
            ? events.AsReadOnly()
            : [];

    public IReadOnlyList<IDomainEvent> GetStoredEvents(KnownCatalogItem knownItem) =>
        eventsByCriteria.TryGetValue(ToPersistentId(knownItem), out var events)
            ? events.AsReadOnly()
            : [];

    public Task<LoadedEventStream<DiscoveryQueryKey, IDomainEvent>> LoadAsync(
        DiscoveryQueryKey streamId,
        CancellationToken cancellationToken)
    {
        _ = cancellationToken;

        return Task.FromResult(eventsByCriteria.TryGetValue(streamId.StableValue, out var events)
            ? new LoadedEventStream<DiscoveryQueryKey, IDomainEvent>(streamId, events.Count, events.ToArray())
            : LoadedEventStream<DiscoveryQueryKey, IDomainEvent>.Empty(streamId));
    }

    public Task<AppendResult<IDomainEvent>> AppendAsync(
        LoadedEventStream<DiscoveryQueryKey, IDomainEvent> stream,
        IReadOnlyList<IDomainEvent> events,
        OperationId? operationId,
        CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        _ = operationId;

        if (!eventsByCriteria.TryGetValue(stream.StreamId.StableValue, out var storedEvents))
        {
            storedEvents = [];
            eventsByCriteria[stream.StreamId.StableValue] = storedEvents;
        }

        if (storedEvents.Count != stream.Version)
        {
            return Task.FromResult(new AppendResult<IDomainEvent>(false, storedEvents.Count, [], AppendOutcome.VersionMismatch));
        }

        storedEvents.AddRange(events);
        return Task.FromResult(new AppendResult<IDomainEvent>(true, storedEvents.Count, events.ToArray(), AppendOutcome.Appended));
    }

    private static string ToPersistentId(MusicSearchCriteria searchCriteria) =>
        DiscoveryQueryKey.StableValueFor(searchCriteria);

    private static string ToPersistentId(KnownCatalogItem knownItem) =>
        DiscoveryQueryKey.StableValueFor(knownItem);
}
