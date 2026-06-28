using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Catalog.Events;
using Soundtrail.Domain.Catalog.Projection;

namespace Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

public sealed class MusicTrackStreamStoreFake :
    IEventStreamRepository<MusicCatalogId, IMusicTrackEvent>
{
    private readonly Dictionary<string, StoredStream> streams = [];

    public IReadOnlyDictionary<string, StoredStream> Streams => streams;

    public void Seed(
        MusicCatalogId musicCatalogId,
        params IMusicTrackEvent[] events)
    {
        streams[musicCatalogId.Value] = new StoredStream
        {
            Version = events.Length
        };

        streams[musicCatalogId.Value].Events.AddRange(events);
    }

    public Task<MusicTrackStream> LoadStreamAsync(
        MusicCatalogId musicCatalogId,
        CancellationToken cancellationToken)
    {
        if (!streams.TryGetValue(musicCatalogId.Value, out var stored))
        {
            return Task.FromResult(new MusicTrackStream(0, Array.Empty<IMusicTrackEvent>()));
        }

        return Task.FromResult(new MusicTrackStream(stored.Version, stored.Events.ToArray()));
    }

    public Task<AppendMusicTrackStreamResult> AppendAsync(
        MusicCatalogId musicCatalogId,
        int expectedVersion,
        CommandId commandId,
        IReadOnlyList<IMusicTrackEvent> events,
        CancellationToken cancellationToken)
    {
        if (!streams.TryGetValue(musicCatalogId.Value, out var stored))
        {
            stored = new StoredStream();
            streams[musicCatalogId.Value] = stored;
        }

        if (stored.AppliedCommandIds.Contains(commandId.Value))
        {
            return Task.FromResult(new AppendMusicTrackStreamResult(false, stored.Version, []));
        }

        if (stored.Version != expectedVersion)
        {
            throw new InvalidOperationException($"Expected version {expectedVersion} but was {stored.Version}.");
        }

        stored.AppliedCommandIds.Add(commandId.Value);
        stored.Events.AddRange(events);
        stored.Version += events.Count;
        return Task.FromResult(new AppendMusicTrackStreamResult(true, stored.Version, events.ToArray()));
    }

    public Task<EventStream<IMusicTrackEvent>> LoadAsync(
        MusicCatalogId streamId,
        CancellationToken cancellationToken)
    {
        _ = cancellationToken;

        if (!streams.TryGetValue(streamId.Value, out var stored))
        {
            return Task.FromResult(new EventStream<IMusicTrackEvent>(0, []));
        }

        return Task.FromResult(new EventStream<IMusicTrackEvent>(stored.Version, stored.Events.ToArray()));
    }

    public Task<AppendResult<IMusicTrackEvent>> AppendAsync(
        AppendRequest<MusicCatalogId, IMusicTrackEvent> request,
        CancellationToken cancellationToken)
    {
        _ = cancellationToken;

        if (!streams.TryGetValue(request.StreamId.Value, out var stored))
        {
            stored = new StoredStream();
            streams[request.StreamId.Value] = stored;
        }

        if (request.OperationId is { } operationId && stored.AppliedCommandIds.Contains(operationId.StableValue))
        {
            return Task.FromResult(new AppendResult<IMusicTrackEvent>(false, stored.Version, [], AppendOutcome.DuplicateOperation));
        }

        if (stored.Version != request.ExpectedVersion)
        {
            return Task.FromResult(new AppendResult<IMusicTrackEvent>(false, stored.Version, [], AppendOutcome.VersionMismatch));
        }

        if (request.OperationId is { } newOperationId)
        {
            stored.AppliedCommandIds.Add(newOperationId.StableValue);
        }

        stored.Events.AddRange(request.Events);
        stored.Version += request.Events.Count;
        return Task.FromResult(new AppendResult<IMusicTrackEvent>(true, stored.Version, request.Events.ToArray(), AppendOutcome.Appended));
    }

    public sealed class StoredStream
    {
        public int Version { get; set; }

        public List<string> AppliedCommandIds { get; } = [];

        public List<IMusicTrackEvent> Events { get; } = [];
    }
}
