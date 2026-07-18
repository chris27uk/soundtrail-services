using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Catalog.Albums;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Catalog.Playlists;
using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Domain.Discovery.Events;
using Soundtrail.Domain.Search;

namespace Soundtrail.Domain.Discovery.Aggregates;

public sealed class DiscoveryHistory
{
    private readonly EventHandlers eventHandlers = new();
    private readonly List<IDomainEvent> uncommittedEvents = [];
    private readonly LoadedEventStream<CatalogWorkId> stream;
    private readonly IEventStreamRepository<CatalogWorkId> repository;
    private readonly SearchRequestContext requestContext;

    private DiscoveryHistory(
        LoadedEventStream<CatalogWorkId> stream, 
        IEventStreamRepository<CatalogWorkId> repository, 
        SearchRequestContext requestContext)
    {
        this.stream = stream;
        this.repository = repository;
        this.requestContext = requestContext;
        this.eventHandlers.Register<WorkRequested>(_ => { });
        foreach (var @event in stream.Events)
        {
            Apply(@event, isNew: false);
        }
    }

    public static async Task<(LoadedEventStream<CatalogWorkId> Stream, DiscoveryHistory Aggregate)> LoadAsync(
        IEventStreamRepository<CatalogWorkId> repository,
        CatalogWorkId streamId,
        SearchRequestContext requestContext,
        CancellationToken cancellationToken)
    {
        var stream = await repository.LoadAsync(streamId, cancellationToken);
        return (stream, new DiscoveryHistory(stream, repository, requestContext));
    }

    public void Request(IEnumerable<EnrichmentTarget> operations)
    {
        foreach (var operation in operations)
        {
            RequestWork(operation);
        }
    }
    
    private void RequestWork(EnrichmentTarget operation)
    {
        Apply(
            new WorkRequested(
                operation,
                this.requestContext.TrustLevel,
                this.requestContext.RiskScore,
                this.requestContext.RequestedAt,
                this.requestContext.CorrelationId),
            isNew: true);
    }

    public async Task SaveAsync(CancellationToken cancellationToken)
    {
        var append = await this.repository.AppendAsync(
            this.stream,
            uncommittedEvents.AsReadOnly(),
            OperationId.From(this.requestContext.CorrelationId),
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
