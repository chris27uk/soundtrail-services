using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions;
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
using Soundtrail.Domain.Search;
using Soundtrail.Services.Enrichment.Orchestrator.Features.Processing.OnLookupCompleted;
using Soundtrail.Services.Tests.Unit.Sociable.Infrastructure.Fakes;

namespace Soundtrail.Services.Tests.Unit.Orchestrator.OnLookupCompleted;

internal sealed class LookupCompletedHandlerUnitTestEnvironment
{
    private LookupCompletedHandlerUnitTestEnvironment(
        EventStreamRepositoryFake repository,
        CommandBusFake commandBus)
    {
        Repository = repository;
        CommandBus = commandBus;
    }

    public EventStreamRepositoryFake Repository { get; }

    public CommandBusFake CommandBus { get; }

    public static LookupCompletedHandlerUnitTestEnvironment Create() =>
        new(new EventStreamRepositoryFake(), new CommandBusFake());

    public LookupCompletedHandler CreateSubject() => new(Repository, CommandBus);

    public static CatalogLookupCompleted CreateStreamingLocationCompleted(
        ArtistId? artistId = null,
        TrackId? trackId = null,
        DateTimeOffset? completedAt = null,
        MessageId? originalCommandId = null)
    {
        var resolvedTrackId = trackId ?? TestTrackIds.Create("lookup-streaming-1");
        var resolvedArtistId = artistId ?? ArtistId.From("artist-lookup-1");
        var when = completedAt ?? new DateTimeOffset(2026, 7, 19, 10, 0, 0, TimeSpan.Zero);

        return new CatalogLookupCompleted(
            MessageId.New(),
            when.AddMinutes(-15),
            CorrelationId.From("corr-streaming-completed"),
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
        MessageId? originalCommandId = null)
    {
        var when = completedAt ?? new DateTimeOffset(2026, 7, 19, 10, 5, 0, TimeSpan.Zero);
        var playlistId = PlaylistId.FromPlaylistName(playlistName);

        return new CatalogLookupCompleted(
            MessageId.New(),
            when.AddMinutes(-15),
            CorrelationId.From("corr-playlist-completed"),
            new LookupResult.Succeeded(
                new LookupResultContext(
                    CatalogWorkId.From(new CatalogItemOperation.ChildTracksForPlaylist(playlistId)),
                    originalCommandId ?? CreateWorkerCommandIdForScheduledWork(
                        Work.DiscoverPlaylistTracks(playlistId),
                        new DateTimeOffset(2026, 7, 19, 9, 50, 30, TimeSpan.Zero),
                        "playlist:Spotify")),
                new LookedUpData.PlaylistTrackReferences([
                    new TrackReference(ArtistName.From("The Travellers"), "Road Song")
                ]),
                when));
    }

    public static CatalogLookupCompleted CreateSearchCompleted(
        string query,
        TrackId trackId,
        DateTimeOffset? completedAt = null,
        MessageId? originalCommandId = null)
    {
        var when = completedAt ?? new DateTimeOffset(2026, 7, 19, 10, 5, 0, TimeSpan.Zero);
        var searchCriteria = new SearchCriteria(query, SearchType.Track);
        var target = Work.SearchExternally(searchCriteria);
        var track = new Track(trackId)
        {
            Title = "Midnight Signals",
            ArtistName = "Aurora Lane",
            AlbumTitle = "Midnight Signals",
            AlbumId = "musicbrainz-artist:aurora-lane:release-midnight-signals",
            DurationMs = 214000,
            ReleaseDate = new DateOnly(2023, 11, 10),
            Mbid = "mbid-midnight-signals-original",
            UpdatedAt = when
        };

        return new CatalogLookupCompleted(
            MessageId.New(),
            when.AddMinutes(-15),
            CorrelationId.From("corr-search-completed"),
            new LookupResult.Succeeded(
                new LookupResultContext(
                    CatalogWorkId.From(searchCriteria),
                    originalCommandId ?? CreateWorkerCommandIdForScheduledWork(
                        target,
                        new DateTimeOffset(2026, 7, 19, 9, 50, 30, TimeSpan.Zero),
                        "musicbrainz-search")),
                new LookedUpData.CatalogEntries([
                    new CatalogDiscoveryEntry(
                        ArtistId.From("musicbrainz-artist:aurora-lane"),
                        new CatalogItem.MusicTrack(track))
                ]),
                when));
    }

    public static CatalogLookupCompleted CreateDeferred(
        DateTimeOffset? completedAt = null,
        DateTimeOffset? deferredUntil = null,
        MessageId? originalCommandId = null)
    {
        var when = completedAt ?? new DateTimeOffset(2026, 7, 19, 10, 10, 0, TimeSpan.Zero);
        var trackId = TestTrackIds.Create("lookup-deferred-1");

        return new CatalogLookupCompleted(
            MessageId.New(),
            when.AddMinutes(-15),
            CorrelationId.From("corr-deferred"),
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

    public static MessageId CreateWorkerCommandIdForScheduledWork(
        EnrichmentTarget target,
        DateTimeOffset scheduledAt,
        string suffix) =>
        MessageId.For(
            $"{MessageId.Deterministic("DispatchLookupWork", target.NormalisedIdentifier, scheduledAt.ToString("O")).Value}:{suffix}");

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

    public void SeedForSearchResult(string query, TrackId trackId)
    {
        var searchCriteria = new SearchCriteria(query, SearchType.Track);
        var target = Work.SearchExternally(searchCriteria);
        SeedEvents = [
            new WorkRequested(
                target,
                LookupPriorityBand.High,
                100,
                0,
                new DateTimeOffset(2026, 7, 19, 9, 50, 0, TimeSpan.Zero),
                CorrelationId.From("corr-search-completed")),
            new WorkScheduled(
                target,
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

}
