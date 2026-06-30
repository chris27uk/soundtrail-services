using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Events;

namespace Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

internal sealed class ArtistCatalogEventRepositoryFake : IEventStreamRepository<ArtistId, IDomainEvent>
{
    private readonly Dictionary<string, StoredStream> streams = [];

    public IReadOnlyDictionary<string, TrackStoredStream> Streams =>
        streams.Values
            .SelectMany(ToTrackStreams)
            .GroupBy(x => x.Key, StringComparer.Ordinal)
            .ToDictionary(
                x => x.Key,
                x =>
                {
                    var combined = new TrackStoredStream();
                    foreach (var stream in x)
                    {
                        combined.Events.AddRange(stream.Value.Events);
                        combined.AppliedCommandIds.AddRange(stream.Value.AppliedCommandIds);
                    }

                    return combined;
                },
                StringComparer.Ordinal);

    public IReadOnlyList<IDomainEvent> GetStoredEvents(ArtistId artistId) =>
        streams.TryGetValue(artistId.Value, out var stored)
            ? stored.Events.AsReadOnly()
            : [];

    public Task<LoadedEventStream<ArtistId, IDomainEvent>> LoadAsync(
        ArtistId streamId,
        CancellationToken cancellationToken)
    {
        _ = cancellationToken;

        return Task.FromResult(streams.TryGetValue(streamId.Value, out var stored)
            ? new LoadedEventStream<ArtistId, IDomainEvent>(streamId, stored.Events.Count, stored.Events.ToArray())
            : LoadedEventStream<ArtistId, IDomainEvent>.Empty(streamId));
    }

    public Task<AppendResult<IDomainEvent>> AppendAsync(
        LoadedEventStream<ArtistId, IDomainEvent> stream,
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

    public sealed class TrackStoredStream
    {
        public List<string> AppliedCommandIds { get; } = [];

        public List<IDomainEvent> Events { get; } = [];
    }

    private static IEnumerable<KeyValuePair<string, TrackStoredStream>> ToTrackStreams(StoredStream stream)
    {
        var byTrack = new Dictionary<string, TrackStoredStream>(StringComparer.Ordinal);
        var sharedEvents = new List<IDomainEvent>();

        foreach (var @event in stream.Events)
        {
            var musicCatalogId = @event switch
            {
                TrackDiscovered discovered => discovered.MusicCatalogId?.Value,
                ProviderReferenceDiscovered reference => reference.MusicCatalogId?.Value,
                ProviderReferenceLookupFailed failed => failed.MusicCatalogId?.Value,
                MetadataCorrected corrected => corrected.MusicCatalogId?.Value,
                StreamingLocationsRequired required => required.MusicCatalogId.Value,
                ArtistDiscovered or AlbumDiscovered => null,
                _ => null
            };

            if (string.IsNullOrWhiteSpace(musicCatalogId))
            {
                if (@event is ArtistDiscovered or AlbumDiscovered)
                {
                    sharedEvents.Add(@event);
                }

                continue;
            }

            if (!byTrack.TryGetValue(musicCatalogId, out var trackStream))
            {
                trackStream = new TrackStoredStream();
                trackStream.AppliedCommandIds.AddRange(stream.AppliedOperationIds);
                byTrack[musicCatalogId] = trackStream;
            }

            trackStream.Events.Add(@event);
        }

        if (sharedEvents.Count > 0)
        {
            foreach (var trackStream in byTrack.Values)
            {
                trackStream.Events.AddRange(sharedEvents);
            }
        }

        return byTrack;
    }
}
