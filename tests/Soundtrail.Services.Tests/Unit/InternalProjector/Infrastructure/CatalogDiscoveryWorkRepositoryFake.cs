using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions.EventSourcing;

namespace Soundtrail.Services.Tests.Unit.InternalProjector.Infrastructure;

internal sealed class CatalogDiscoveryWorkRepositoryFake : IEventStreamRepository<MusicCatalogId, IDomainEvent>
{
    private readonly Dictionary<string, StoredStream> streams = [];

    public IReadOnlyList<IDomainEvent> GetStoredEvents(MusicCatalogId musicCatalogId) =>
        streams.TryGetValue(musicCatalogId.Value, out var stored)
            ? stored.Events.AsReadOnly()
            : [];

    public Task<LoadedEventStream<MusicCatalogId, IDomainEvent>> LoadAsync(
        MusicCatalogId streamId,
        CancellationToken cancellationToken)
    {
        _ = cancellationToken;

        return Task.FromResult(streams.TryGetValue(streamId.Value, out var stored)
            ? new LoadedEventStream<MusicCatalogId, IDomainEvent>(streamId, stored.Events.Count, stored.Events.ToArray())
            : LoadedEventStream<MusicCatalogId, IDomainEvent>.Empty(streamId));
    }

    public Task<AppendResult<IDomainEvent>> AppendAsync(
        LoadedEventStream<MusicCatalogId, IDomainEvent> stream,
        IReadOnlyList<IDomainEvent> events,
        OperationId? operationId,
        CancellationToken cancellationToken)
    {
        _ = cancellationToken;

        if (!streams.TryGetValue(stream.StreamId.Value, out var stored))
        {
            stored = new StoredStream();
            streams[stream.StreamId.Value] = stored;
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
