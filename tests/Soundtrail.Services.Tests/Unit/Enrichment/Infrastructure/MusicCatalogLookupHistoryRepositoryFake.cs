using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Enrichment;

namespace Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

internal sealed class MusicCatalogLookupHistoryRepositoryFake : IEventStreamRepository<MusicCatalogLookupId, IDomainEvent>
{
    private readonly Dictionary<string, StoredStream> streams = [];

    public IReadOnlyList<IDomainEvent> GetStoredEvents(MusicCatalogLookupId streamId) =>
        streams.TryGetValue(streamId.StableValue, out var stored)
            ? stored.Events.AsReadOnly()
            : [];

    public Task<LoadedEventStream<MusicCatalogLookupId, IDomainEvent>> LoadAsync(
        MusicCatalogLookupId streamId,
        CancellationToken cancellationToken)
    {
        _ = cancellationToken;

        return Task.FromResult(streams.TryGetValue(streamId.StableValue, out var stored)
            ? new LoadedEventStream<MusicCatalogLookupId, IDomainEvent>(streamId, stored.Events.Count, stored.Events.ToArray())
            : LoadedEventStream<MusicCatalogLookupId, IDomainEvent>.Empty(streamId));
    }

    public Task<AppendResult<IDomainEvent>> AppendAsync(
        LoadedEventStream<MusicCatalogLookupId, IDomainEvent> stream,
        IReadOnlyList<IDomainEvent> events,
        OperationId? operationId,
        CancellationToken cancellationToken)
    {
        _ = cancellationToken;

        if (!streams.TryGetValue(stream.StreamId.StableValue, out var stored))
        {
            stored = new StoredStream();
            streams[stream.StreamId.StableValue] = stored;
        }

        if (operationId is { } duplicate && stored.AppliedOperationIds.Contains(duplicate.StableValue))
        {
            return Task.FromResult(new AppendResult<IDomainEvent>(false, stored.Events.Count, [], AppendOutcome.DuplicateOperation));
        }

        if (stored.Events.Count != stream.Version)
        {
            return Task.FromResult(new AppendResult<IDomainEvent>(false, stored.Events.Count, [], AppendOutcome.VersionMismatch));
        }

        stored.Events.AddRange(events);
        if (operationId is { } applied)
        {
            stored.AppliedOperationIds.Add(applied.StableValue);
        }

        return Task.FromResult(new AppendResult<IDomainEvent>(true, stored.Events.Count, events.ToArray(), AppendOutcome.Appended));
    }

    private sealed class StoredStream
    {
        public List<IDomainEvent> Events { get; } = [];

        public HashSet<string> AppliedOperationIds { get; } = new(StringComparer.Ordinal);
    }
}
