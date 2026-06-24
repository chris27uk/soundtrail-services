using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Events;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnMusicCatalogLookupAttempted.Support;

namespace Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

internal sealed class CatalogSearchDiscoveryRepositoryFake : ICompleteTrackedDiscoveriesRepository
{
    private readonly Dictionary<string, List<IDomainEvent>> eventsByCriteria = [];

    public void Seed(
        CatalogSearchCriteria criteria,
        params IDomainEvent[] events)
    {
        eventsByCriteria[criteria.Value] = events.ToList();
    }

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
