using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Discovery.Candidates;
using Soundtrail.Domain.Discovery.Events;
using Soundtrail.Domain.Search;

namespace Soundtrail.Domain.Discovery.Aggregates;

public sealed class DiscoveryHistory
{
    private readonly EventHandlers eventHandlers = new();
    private readonly List<IDomainEvent> uncommittedEvents = [];
    private readonly SearchRequestContext requestContext;

    private DiscoveryHistory(IEnumerable<IDomainEvent> events, SearchRequestContext requestContext)
    {
        this.requestContext = requestContext;
        eventHandlers.Register<WorkRequested>(_ => { });

        foreach (var @event in events)
        {
            Apply(@event, isNew: false);
        }
    }

    public static async Task<(LoadedEventStream<CatalogSearchId, IDomainEvent> Stream, DiscoveryHistory Aggregate)> LoadAsync(
        IEventStreamRepository<CatalogSearchId, IDomainEvent> repository,
        SearchCriteria searchCriteria,
        SearchRequestContext requestContext,
        CancellationToken cancellationToken)
    {
        var streamId = CatalogSearchId.From(searchCriteria);
        var stream = await repository.LoadAsync(streamId, cancellationToken);
        return (stream, new DiscoveryHistory(stream.Events, requestContext));
    }

    public void NewSearchWithExistingCatalogItems(IEnumerable<ScoredCandidate> candidates)
    {
        foreach (var candidate in candidates)
        {
            Apply(
                new WorkRequested(
                    new EnrichmentTarget.Existing(candidate.Id),
                    requestContext.TrustLevel,
                    requestContext.RiskScore,
                    requestContext.RequestedAt,
                    requestContext.CorrelationId),
                isNew: true);
        }
    }

    public void NewSearch(SearchCriteria searchCriteria)
    {
        Apply(
            new WorkRequested(
                new EnrichmentTarget.Unknown(searchCriteria),
                requestContext.TrustLevel,
                requestContext.RiskScore,
                requestContext.RequestedAt,
                requestContext.CorrelationId),
            isNew: true);
    }

    public async Task SaveAsync(
        IEventStreamRepository<CatalogSearchId, IDomainEvent> repository,
        LoadedEventStream<CatalogSearchId, IDomainEvent> stream,
        CommandId commandId,
        CancellationToken cancellationToken)
    {
        var append = await repository.AppendAsync(
            stream,
            uncommittedEvents.AsReadOnly(),
            OperationId.From(commandId.Value),
            cancellationToken);

        if (append.Outcome == AppendOutcome.VersionMismatch)
        {
            throw new InvalidOperationException($"Catalog search stream concurrency conflict for '{stream.StreamId.StableValue}'.");
        }

        if (append.Appended || append.Outcome == AppendOutcome.DuplicateOperation)
        {
            this.uncommittedEvents.Clear();
        }
    }

    private void Apply(IDomainEvent @event, bool isNew)
    {
        eventHandlers.Handle(@event);

        if (isNew)
        {
            uncommittedEvents.Add(@event);
        }
    }

    public sealed record SearchRequestContext(
        int TrustLevel,
        int RiskScore,
        DateTimeOffset RequestedAt,
        CorrelationId CorrelationId);
}
