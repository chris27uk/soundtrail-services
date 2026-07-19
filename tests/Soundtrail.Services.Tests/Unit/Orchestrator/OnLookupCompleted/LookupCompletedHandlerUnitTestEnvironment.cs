using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Catalog.Playlists;
using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Aggregates;
using Soundtrail.Domain.Discovery.Events;
using Soundtrail.Domain.Discovery.Messages;
using Soundtrail.Services.Enrichment.Orchestrator.Features.Processing.OnLookupCompleted;

namespace Soundtrail.Services.Tests.Unit.Orchestrator.OnLookupCompleted;

internal sealed class LookupCompletedHandlerUnitTestEnvironment
{
    private LookupCompletedHandlerUnitTestEnvironment(
        EventStreamRepositoryFake repository)
    {
        Repository = repository;
    }

    public EventStreamRepositoryFake Repository { get; }

    public static LookupCompletedHandlerUnitTestEnvironment Create() =>
        new(new EventStreamRepositoryFake());

    public LookupCompletedHandler CreateSubject() => new(Repository);

    public static CatalogLookupCompleted CreateStreamingLocationCompleted(
        ArtistId? artistId = null,
        TrackId? trackId = null,
        DateTimeOffset? completedAt = null,
        CommandId? originalCommandId = null)
    {
        var resolvedTrackId = trackId ?? TestTrackIds.Create("lookup-streaming-1");
        var resolvedArtistId = artistId ?? ArtistId.From("artist-lookup-1");
        var when = completedAt ?? new DateTimeOffset(2026, 7, 19, 10, 0, 0, TimeSpan.Zero);

        return new CatalogLookupCompleted(
            new LookupResult.Succeeded(
                new LookupResultContext(
                    CatalogWorkId.From(new CatalogItemOperation.StreamingLocationForTrack(resolvedTrackId)),
                    originalCommandId ?? CreateWorkerCommandIdForScheduledWork(
                        Work.EnrichTrackStreamingLocation(resolvedTrackId),
                        new DateTimeOffset(2026, 7, 19, 9, 45, 30, TimeSpan.Zero),
                        "streaming-isrc:Spotify")),
                new LookedUpData.TrackStreamingLink(
                    resolvedArtistId,
                    resolvedTrackId,
                    new StreamingLocation(
                        ProviderName.Spotify,
                        "spotify:track:123",
                        new Uri("https://open.spotify.com/track/123"),
                        LookupSource.Odesli,
                        when)),
                when));
    }

    public static CatalogLookupCompleted CreatePlaylistCompleted(
        string playlistName = "Road Trip",
        DateTimeOffset? completedAt = null,
        CommandId? originalCommandId = null)
    {
        var when = completedAt ?? new DateTimeOffset(2026, 7, 19, 10, 5, 0, TimeSpan.Zero);
        var playlistId = PlaylistId.FromPlaylistName(playlistName);
        var track = CreateTrack("playlist-track-1");

        return new CatalogLookupCompleted(
            new LookupResult.Succeeded(
                new LookupResultContext(
                    CatalogWorkId.From(new CatalogItemOperation.ChildTracksForPlaylist(playlistId)),
                    originalCommandId ?? CreateWorkerCommandIdForScheduledWork(
                        Work.DiscoverPlaylistTracks(playlistId),
                        new DateTimeOffset(2026, 7, 19, 9, 50, 30, TimeSpan.Zero),
                        "playlist:Spotify")),
                new LookedUpData.CatalogEntries([
                    new CatalogDiscoveryEntry(
                        ArtistId.From("artist-playlist-1"),
                        new CatalogItem.MusicTrack(track))
                ]),
                when));
    }

    public static CatalogLookupCompleted CreateDeferred(
        DateTimeOffset? completedAt = null,
        DateTimeOffset? deferredUntil = null,
        CommandId? originalCommandId = null)
    {
        var when = completedAt ?? new DateTimeOffset(2026, 7, 19, 10, 10, 0, TimeSpan.Zero);
        var trackId = TestTrackIds.Create("lookup-deferred-1");

        return new CatalogLookupCompleted(
            new LookupResult.Deferred(
                new LookupResultContext(
                    CatalogWorkId.From(new CatalogItemOperation.StreamingLocationForTrack(trackId)),
                    originalCommandId ?? CreateWorkerCommandIdForScheduledWork(
                        Work.EnrichTrackStreamingLocation(trackId),
                        new DateTimeOffset(2026, 7, 19, 9, 45, 30, TimeSpan.Zero),
                        "streaming-isrc:Spotify")),
                deferredUntil ?? when.AddMinutes(15),
                "Rate limited.",
                when));
    }

    public void SeedForStreamingLocation(TrackId? trackId = null)
    {
        var resolvedTrackId = trackId ?? TestTrackIds.Create("lookup-streaming-1");
        SeedEvents = [
            new WorkRequested(
                Work.EnrichTrackStreamingLocation(resolvedTrackId),
                LookupPriorityBand.Low,
                50,
                5,
                new DateTimeOffset(2026, 7, 19, 9, 45, 0, TimeSpan.Zero),
                CorrelationId.From("corr-streaming-completed")),
            new WorkScheduled(
                Work.EnrichTrackStreamingLocation(resolvedTrackId),
                LookupPriorityBand.Low,
                new DateTimeOffset(2026, 7, 19, 9, 46, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 7, 19, 9, 50, 0, TimeSpan.Zero),
                "Scheduled.",
                new DateTimeOffset(2026, 7, 19, 9, 45, 30, TimeSpan.Zero))
        ];
    }

    public void SeedWithMultipleScheduledStreamingLookups(TrackId firstTrackId, TrackId secondTrackId)
    {
        SeedEvents =
        [
            new WorkRequested(
                Work.EnrichTrackStreamingLocation(firstTrackId),
                LookupPriorityBand.Low,
                50,
                5,
                new DateTimeOffset(2026, 7, 19, 9, 40, 0, TimeSpan.Zero),
                CorrelationId.From("corr-first")),
            new WorkScheduled(
                Work.EnrichTrackStreamingLocation(firstTrackId),
                LookupPriorityBand.Low,
                new DateTimeOffset(2026, 7, 19, 9, 41, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 7, 19, 9, 45, 0, TimeSpan.Zero),
                "Scheduled first.",
                new DateTimeOffset(2026, 7, 19, 9, 40, 30, TimeSpan.Zero)),
            new WorkRequested(
                Work.EnrichTrackStreamingLocation(secondTrackId),
                LookupPriorityBand.High,
                90,
                1,
                new DateTimeOffset(2026, 7, 19, 9, 50, 0, TimeSpan.Zero),
                CorrelationId.From("corr-second")),
            new WorkScheduled(
                Work.EnrichTrackStreamingLocation(secondTrackId),
                LookupPriorityBand.High,
                new DateTimeOffset(2026, 7, 19, 9, 51, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 7, 19, 9, 55, 0, TimeSpan.Zero),
                "Scheduled second.",
                new DateTimeOffset(2026, 7, 19, 9, 50, 30, TimeSpan.Zero))
        ];
    }

    public static CommandId CreateWorkerCommandIdForScheduledWork(
        EnrichmentTarget target,
        DateTimeOffset scheduledAt,
        string suffix) =>
        CommandId.For($"DispatchLookupWork:{target.NormalisedIdentifier}:{scheduledAt:O}:{suffix}");

    public void SeedForPlaylist(string playlistName = "Road Trip")
    {
        var playlistId = PlaylistId.FromPlaylistName(playlistName);
        SeedEvents = [
            new WorkRequested(
                Work.DiscoverPlaylistTracks(playlistId),
                LookupPriorityBand.High,
                80,
                2,
                new DateTimeOffset(2026, 7, 19, 9, 50, 0, TimeSpan.Zero),
                CorrelationId.From("corr-playlist-completed")),
            new WorkScheduled(
                Work.DiscoverPlaylistTracks(playlistId),
                LookupPriorityBand.High,
                new DateTimeOffset(2026, 7, 19, 9, 51, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 7, 19, 9, 55, 0, TimeSpan.Zero),
                "Scheduled.",
                new DateTimeOffset(2026, 7, 19, 9, 50, 30, TimeSpan.Zero))
        ];
    }

    private IReadOnlyList<IDomainEvent> SeedEvents
    {
        set => Repository.SeedEvents = value;
    }

    private static Track CreateTrack(string seed)
    {
        var track = new Track(TestTrackIds.Create(seed))
        {
            Title = "Road Song",
            ArtistName = "The Travellers",
            AlbumTitle = "Miles Ahead",
            ReleaseDate = new DateOnly(2020, 1, 1),
            ReleaseType = "studio"
        };

        return track;
    }

    public sealed class EventStreamRepositoryFake : IEventStreamRepository<CatalogWorkId>
    {
        public IReadOnlyList<IDomainEvent> SeedEvents { get; set; } = [];

        public IReadOnlyList<IDomainEvent> AppendedEvents { get; private set; } = [];

        public int LoadCalls { get; private set; }

        public Task<LoadedEventStream<CatalogWorkId>> LoadAsync(CatalogWorkId streamId, CancellationToken cancellationToken)
        {
            LoadCalls++;
            return Task.FromResult(
                SeedEvents.Count == 0
                    ? LoadedEventStream<CatalogWorkId>.Empty(streamId)
                    : new LoadedEventStream<CatalogWorkId>(streamId, SeedEvents.Count, SeedEvents));
        }

        public Task<AppendResult> AppendAsync(
            LoadedEventStream<CatalogWorkId> stream,
            IReadOnlyList<IDomainEvent> events,
            OperationId? operationId,
            CancellationToken cancellationToken)
        {
            AppendedEvents = events.ToArray();
            return Task.FromResult(new AppendResult(true, stream.Version + events.Count, events.ToArray(), AppendOutcome.Appended));
        }
    }
}
