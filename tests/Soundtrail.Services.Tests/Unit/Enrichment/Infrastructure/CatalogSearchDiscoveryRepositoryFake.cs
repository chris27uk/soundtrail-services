using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Events;

namespace Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

internal sealed class CatalogSearchDiscoveryRepositoryFake : ICatalogSearchDiscoveryRepository
{
    private readonly Dictionary<string, List<IDomainEvent>> eventsByCriteria = [];

    public Task<CatalogSearchDiscoveryEventStream> LoadAsync(
        CatalogSearchCriteria criteria,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(eventsByCriteria.TryGetValue(criteria.Value, out var events)
            ? new CatalogSearchDiscoveryEventStream(events.Count, events.ToArray())
            : new CatalogSearchDiscoveryEventStream(0, []));
    }

    public Task<bool> AppendAsync(
        CatalogSearchCriteria criteria,
        int expectedVersion,
        IReadOnlyCollection<IDomainEvent> events,
        CancellationToken cancellationToken)
    {
        if (!eventsByCriteria.TryGetValue(criteria.Value, out var storedEvents))
        {
            storedEvents = [];
            eventsByCriteria[criteria.Value] = storedEvents;
        }

        if (storedEvents.Count != expectedVersion)
        {
            return Task.FromResult(false);
        }

        storedEvents.AddRange(events);
        return Task.FromResult(true);
    }

    public IReadOnlyList<IDomainEvent> GetStoredEvents(CatalogSearchCriteria criteria) =>
        eventsByCriteria.TryGetValue(criteria.Value, out var events)
            ? events.AsReadOnly()
            : [];
}
