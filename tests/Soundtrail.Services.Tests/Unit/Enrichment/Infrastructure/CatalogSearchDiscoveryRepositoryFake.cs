using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Events;
using Soundtrail.Domain.Model;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnMusicCatalogLookupAttempted.Support;
using Soundtrail.Translators.Discovery;

namespace Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

internal sealed class CatalogSearchDiscoveryRepositoryFake : ICompleteTrackedDiscoveriesRepository
{
    private readonly Dictionary<string, List<IDomainEvent>> eventsByCriteria = [];

    public void Seed(
        MusicSearchCriteria searchCriteria,
        params IDomainEvent[] events) =>
        Seed(MusicSeekOrSearchCriteria.FromSearch(searchCriteria), events);

    public void Seed(
        MusicSeekOrSearchCriteria criteria,
        params IDomainEvent[] events)
    {
        eventsByCriteria[ToPersistentId(criteria)] = events.ToList();
    }

    public Task<CatalogSearchDiscoveryEventStream> LoadAsync(
        MusicSearchCriteria searchCriteria,
        CancellationToken cancellationToken) =>
        LoadAsync(MusicSeekOrSearchCriteria.FromSearch(searchCriteria), cancellationToken);

    public Task<CatalogSearchDiscoveryEventStream> LoadAsync(
        MusicSeekOrSearchCriteria criteria,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(eventsByCriteria.TryGetValue(ToPersistentId(criteria), out var events)
            ? new CatalogSearchDiscoveryEventStream(events.Count, events.ToArray())
            : new CatalogSearchDiscoveryEventStream(0, []));
    }

    public Task<bool> AppendAsync(
        MusicSearchCriteria searchCriteria,
        int expectedVersion,
        IReadOnlyCollection<IDomainEvent> events,
        CancellationToken cancellationToken) =>
        AppendAsync(MusicSeekOrSearchCriteria.FromSearch(searchCriteria), expectedVersion, events, cancellationToken);

    public Task<bool> AppendAsync(
        MusicSeekOrSearchCriteria criteria,
        int expectedVersion,
        IReadOnlyCollection<IDomainEvent> events,
        CancellationToken cancellationToken)
    {
        var persistentId = ToPersistentId(criteria);
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
        GetStoredEvents(MusicSeekOrSearchCriteria.FromSearch(searchCriteria));

    public IReadOnlyList<IDomainEvent> GetStoredEvents(MusicSeekOrSearchCriteria criteria) =>
        eventsByCriteria.TryGetValue(ToPersistentId(criteria), out var events)
            ? events.AsReadOnly()
            : [];

    private static string ToPersistentId(MusicSeekOrSearchCriteria criteria) =>
        MusicSearchTermPersistentIdTranslator.ToPersistentId(criteria);
}
