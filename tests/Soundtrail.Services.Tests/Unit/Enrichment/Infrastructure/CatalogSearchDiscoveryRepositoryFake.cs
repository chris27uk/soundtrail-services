using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Commands;

namespace Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

internal sealed class CatalogSearchDiscoveryRepositoryFake : ICatalogSearchDiscoveryRepository
{
    private readonly Dictionary<string, List<IDomainEvent>> eventsByCriteria = [];

    public void Seed(
        MusicSearchCriteria searchCriteria,
        params IDomainEvent[] events)
    {
        eventsByCriteria[ToPersistentId(searchCriteria)] = events.ToList();
    }

    public Task<CatalogSearchDiscoveryEventStream> LoadAsync(
        MusicSearchCriteria searchCriteria,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(eventsByCriteria.TryGetValue(ToPersistentId(searchCriteria), out var events)
            ? new CatalogSearchDiscoveryEventStream(events.Count, events.ToArray())
            : new CatalogSearchDiscoveryEventStream(0, []));
    }

    public Task<CatalogSearchDiscoveryEventStream> LoadAsync(
        KnownCatalogItem knownItem,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(eventsByCriteria.TryGetValue(ToPersistentId(knownItem), out var events)
            ? new CatalogSearchDiscoveryEventStream(events.Count, events.ToArray())
            : new CatalogSearchDiscoveryEventStream(0, []));
    }

    public Task<bool> AppendAsync(
        MusicSearchCriteria searchCriteria,
        int expectedVersion,
        IReadOnlyCollection<IDomainEvent> events,
        CancellationToken cancellationToken)
    {
        var persistentId = ToPersistentId(searchCriteria);
        if (!eventsByCriteria.TryGetValue(persistentId, out var storedEvents))
        {
            storedEvents = [];
            eventsByCriteria[persistentId] = storedEvents;
        }

        if (storedEvents.Count != expectedVersion)
        {
            return Task.FromResult(false);
        }

        storedEvents.AddRange(events);
        return Task.FromResult(true);
    }

    public Task<bool> AppendAsync(
        KnownCatalogItem knownItem,
        int expectedVersion,
        IReadOnlyCollection<IDomainEvent> events,
        CancellationToken cancellationToken)
    {
        var persistentId = ToPersistentId(knownItem);
        if (!eventsByCriteria.TryGetValue(persistentId, out var storedEvents))
        {
            storedEvents = [];
            eventsByCriteria[persistentId] = storedEvents;
        }

        if (storedEvents.Count != expectedVersion)
        {
            return Task.FromResult(false);
        }

        storedEvents.AddRange(events);
        return Task.FromResult(true);
    }

    public IReadOnlyList<IDomainEvent> GetStoredEvents(MusicSearchCriteria searchCriteria) =>
        eventsByCriteria.TryGetValue(ToPersistentId(searchCriteria), out var events)
            ? events.AsReadOnly()
            : [];

    public IReadOnlyList<IDomainEvent> GetStoredEvents(KnownCatalogItem knownItem) =>
        eventsByCriteria.TryGetValue(ToPersistentId(knownItem), out var events)
            ? events.AsReadOnly()
            : [];

    private static string ToPersistentId(MusicSearchCriteria searchCriteria) =>
        MusicSearchTermPersistentIdTranslator.ToPersistentId(searchCriteria);

    private static string ToPersistentId(KnownCatalogItem knownItem) =>
        MusicSearchTermPersistentIdTranslator.ToPersistentId(knownItem);
}
