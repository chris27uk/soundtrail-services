using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Discovery.Events;
using Soundtrail.Domain.Search;

namespace Soundtrail.Domain.Discovery;

public sealed class CatalogSearchCandidates
{
    private readonly EventHandlers<CatalogSearchCandidates> eventHandlers;
    private readonly List<IDomainEvent> uncommittedEvents = [];
    private MusicSearchCriteria? criteria;

    private CatalogSearchCandidates(IEnumerable<IDomainEvent> events)
    {
        eventHandlers = CreateHandlers();

        foreach (var @event in events)
        {
            Apply(@event, isNew: false);
        }
    }

    public static async Task<(LoadedEventStream<DiscoveryQueryKey, IDomainEvent> Stream, CatalogSearchCandidates Aggregate)> LoadAsync(
        IEventStreamRepository<DiscoveryQueryKey, IDomainEvent> repository,
        MusicSearchCriteria criteria,
        CancellationToken cancellationToken)
    {
        var stream = await repository.LoadAsync(DiscoveryQueryKey.For(criteria), cancellationToken);
        var aggregate = new CatalogSearchCandidates(stream.Events.OfType<CatalogSearchCandidateRecorded>());
        aggregate.criteria ??= criteria;
        return (stream, aggregate);
    }

    public void Record(
        MusicCatalogId musicCatalogId,
        int trustLevel,
        int riskScore,
        DateTimeOffset startedAt,
        CorrelationId correlationId)
    {
        Apply(
            new CatalogSearchCandidateRecorded(
                RequireCriteria(),
                musicCatalogId,
                trustLevel,
                riskScore,
                startedAt,
                correlationId),
            isNew: true);
    }

    public async Task<bool> SaveAsync(
        IEventStreamRepository<DiscoveryQueryKey, IDomainEvent> repository,
        LoadedEventStream<DiscoveryQueryKey, IDomainEvent> stream,
        CancellationToken cancellationToken)
    {
        if (uncommittedEvents.Count == 0)
        {
            return true;
        }

        var saved = (await repository.AppendAsync(
            stream,
            uncommittedEvents.AsReadOnly(),
            null,
            cancellationToken)).Appended;

        if (saved)
        {
            uncommittedEvents.Clear();
        }

        return saved;
    }

    private void Apply(IDomainEvent @event, bool isNew)
    {
        eventHandlers.Handle(@event);

        if (isNew)
        {
            uncommittedEvents.Add(@event);
        }
    }

    private MusicSearchCriteria RequireCriteria() =>
        criteria ?? throw new InvalidOperationException("Catalog search criteria has not been established.");

    private EventHandlers<CatalogSearchCandidates> CreateHandlers()
    {
        var handlers = new EventHandlers<CatalogSearchCandidates>();
        handlers.Register<CatalogSearchCandidateRecorded>(On);
        return handlers;
    }

    private void On(CatalogSearchCandidateRecorded @event)
    {
        criteria = @event.SearchCriteria;
    }
}
