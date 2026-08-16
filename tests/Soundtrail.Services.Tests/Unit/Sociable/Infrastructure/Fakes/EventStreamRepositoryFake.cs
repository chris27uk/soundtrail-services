using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery.Aggregates;

namespace Soundtrail.Services.Tests.Unit.Sociable.Infrastructure.Fakes;

internal sealed class EventStreamRepositoryFake : IEventStreamRepository<CatalogWorkId>
{
    public LoadedEventStream<CatalogWorkId>? LoadedStream { get; private set; }

    public IReadOnlyList<IDomainEvent> SeedEvents { get; set; } = [];

    public IReadOnlyList<IDomainEvent> AppendedEvents { get; private set; } = [];

    public int LoadCalls { get; private set; }

    public OperationId? LastOperationId { get; private set; }

    public Task<LoadedEventStream<CatalogWorkId>> LoadAsync(
        CatalogWorkId streamId,
        CancellationToken cancellationToken)
    {
        LoadCalls++;
        LoadedStream = SeedEvents.Count == 0
            ? LoadedEventStream<CatalogWorkId>.Empty(streamId)
            : new LoadedEventStream<CatalogWorkId>(streamId, SeedEvents.Count, SeedEvents);
        return Task.FromResult(LoadedStream);
    }

    public Task<AppendResult> AppendAsync(
        LoadedEventStream<CatalogWorkId> stream,
        IReadOnlyList<IDomainEvent> events,
        OperationId? operationId,
        CancellationToken cancellationToken,
        ProjectionHint? projectionHint = null)
    {
        LastOperationId = operationId;
        AppendedEvents = events.ToArray();
        return Task.FromResult(new AppendResult(true, stream.Version + events.Count, events.ToArray(), AppendOutcome.Appended));
    }
}
