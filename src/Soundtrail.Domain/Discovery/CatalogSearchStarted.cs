using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Events;

namespace Soundtrail.Domain.Discovery;

public sealed class CatalogSearchStarted
{
    private readonly EventHandlers<CatalogSearchStarted> eventHandlers;
    private readonly List<IDomainEvent> uncommittedEvents = [];
    private CatalogSearchCriteria? criteria;
    private int version;

    private CatalogSearchStarted(IEnumerable<IDomainEvent> events, int version)
    {
        eventHandlers = CreateHandlers();

        foreach (var @event in events)
        {
            Apply(@event, isNew: false);
        }

        this.version = version;
    }

    public static async Task<CatalogSearchStarted> LoadAsync(
        ICatalogSearchDiscoveryRepository repository,
        CatalogSearchCriteria criteria,
        CancellationToken cancellationToken)
    {
        var stream = await repository.LoadAsync(criteria, cancellationToken);
        var aggregate = new CatalogSearchStarted(stream.Events.OfType<MusicTrackSearchStarted>(), stream.Version);
        aggregate.criteria ??= criteria;
        return aggregate;
    }

    public void Record(
        MusicCatalogId musicCatalogId,
        int trustLevel,
        int riskScore,
        DateTimeOffset startedAt,
        CorrelationId correlationId)
    {
        Apply(
            new MusicTrackSearchStarted(
                RequireCriteria(),
                musicCatalogId,
                trustLevel,
                riskScore,
                startedAt,
                correlationId),
            isNew: true);
    }

    public async Task<bool> SaveAsync(
        ICatalogSearchDiscoveryRepository repository,
        CancellationToken cancellationToken)
    {
        if (uncommittedEvents.Count == 0)
        {
            return true;
        }

        var saved = await repository.AppendAsync(
            RequireCriteria(),
            version,
            uncommittedEvents.AsReadOnly(),
            cancellationToken);

        if (saved)
        {
            version += uncommittedEvents.Count;
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

    private CatalogSearchCriteria RequireCriteria() =>
        criteria ?? throw new InvalidOperationException("Catalog search criteria has not been established.");

    private EventHandlers<CatalogSearchStarted> CreateHandlers()
    {
        var handlers = new EventHandlers<CatalogSearchStarted>();
        handlers.Register<MusicTrackSearchStarted>(On);
        return handlers;
    }

    private void On(MusicTrackSearchStarted @event)
    {
        criteria = @event.Criteria;
    }
}
