using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Events;
using Soundtrail.Domain.Model;

namespace Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

public sealed class MusicTrackStreamStoreFake : IMusicTrackEventRepository
{
    private readonly Dictionary<string, StoredStream> streams = [];

    public IReadOnlyDictionary<string, StoredStream> Streams => streams;

    public void Seed(
        MusicCatalogId musicCatalogId,
        params MusicTrackFact[] facts)
    {
        streams[musicCatalogId.Value] = new StoredStream
        {
            Version = facts.Length
        };

        streams[musicCatalogId.Value].Facts.AddRange(facts);
    }

    public Task<MusicTrackStream> LoadEventsAsync(
        MusicCatalogId musicCatalogId,
        CancellationToken cancellationToken)
    {
        if (!streams.TryGetValue(musicCatalogId.Value, out var stored))
        {
            return Task.FromResult(new MusicTrackStream(0, Array.Empty<MusicTrackFact>()));
        }

        return Task.FromResult(new MusicTrackStream(stored.Version, stored.Facts.ToArray()));
    }

    public Task<AppendMusicTrackStreamResult> AppendEventsAsync(
        MusicCatalogId musicCatalogId,
        int expectedVersion,
        CommandId commandId,
        IReadOnlyList<MusicTrackFact> events,
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
        stored.Facts.AddRange(events);
        stored.Version += events.Count;
        return Task.FromResult(new AppendMusicTrackStreamResult(true, stored.Version, events.ToArray()));
    }

    public sealed class StoredStream
    {
        public int Version { get; set; }

        public List<string> AppliedCommandIds { get; } = [];

        public List<MusicTrackFact> Facts { get; } = [];
    }
}
