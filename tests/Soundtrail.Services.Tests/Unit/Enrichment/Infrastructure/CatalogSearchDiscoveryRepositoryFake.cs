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
        eventsByCriteria[ToPersistentId(searchCriteria)] = events.ToList();
    }

    public IReadOnlyList<IDomainEvent> GetStoredEvents(MusicSearchCriteria searchCriteria) =>
        eventsByCriteria.TryGetValue(ToPersistentId(searchCriteria), out var events)
            ? events.AsReadOnly()
            : [];

    public IReadOnlyList<IDomainEvent> GetStoredEvents(KnownCatalogItem knownItem) =>
        eventsByCriteria.TryGetValue(ToPersistentId(knownItem), out var events)
            ? events.AsReadOnly()
            : [];

    public Task<EventStream<IDomainEvent>> LoadAsync(
        DiscoveryQueryKey streamId,
        CancellationToken cancellationToken)
    {
        _ = cancellationToken;

        return Task.FromResult(eventsByCriteria.TryGetValue(streamId.StableValue, out var events)
            ? new EventStream<IDomainEvent>(events.Count, events.ToArray())
            : new EventStream<IDomainEvent>(0, []));
    }

    public Task<AppendResult<IDomainEvent>> AppendAsync(
        AppendRequest<DiscoveryQueryKey, IDomainEvent> request,
        CancellationToken cancellationToken)
    {
        _ = cancellationToken;

        if (!eventsByCriteria.TryGetValue(request.StreamId.StableValue, out var storedEvents))
        {
            storedEvents = [];
            eventsByCriteria[request.StreamId.StableValue] = storedEvents;
        }

        if (storedEvents.Count != request.ExpectedVersion)
        {
            return Task.FromResult(new AppendResult<IDomainEvent>(false, storedEvents.Count, [], AppendOutcome.VersionMismatch));
        }

        storedEvents.AddRange(request.Events);
        return Task.FromResult(new AppendResult<IDomainEvent>(true, storedEvents.Count, request.Events.ToArray(), AppendOutcome.Appended));
    }

    private static string ToPersistentId(MusicSearchCriteria searchCriteria) =>
        DiscoveryQueryKey.StableValueFor(searchCriteria);

    private static string ToPersistentId(KnownCatalogItem knownItem) =>
        DiscoveryQueryKey.StableValueFor(knownItem);
}
