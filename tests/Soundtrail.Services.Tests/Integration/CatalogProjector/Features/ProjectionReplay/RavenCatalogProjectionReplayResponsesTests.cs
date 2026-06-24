using FluentAssertions;
using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Events;
using Soundtrail.Services.Tests.Integration.Api.Infrastructure;
using Soundtrail.Translators.MusicTrackEventStore;

namespace Soundtrail.Services.Tests.Integration.CatalogProjector.Features.ProjectionReplay;

[Collection(RavenEmbeddedCollection.Name)]
public sealed class RavenCatalogProjectionReplayResponsesTests
{
    private static readonly IMusicTrackStoredEventRecordTranslator Translator = MusicTrackStoredEventRecordTranslator.Default;

    [Fact]
    public async Task Given_Replayable_MusicTrack_Events_When_Projecting_Then_Catalog_Documents_Are_Materialized_And_Searchable()
    {
        await using var env = RavenCatalogProjectionReplayTestEnvironment.Create();

        await env.ApplyAsync(
            MinimalTrackInfo("mc_track_1", 1, "Mr. Brightside", "The Killers"),
            ArtistDiscovered("mc_track_1", 2, "artist_the_killers", "The Killers"),
            AlbumDiscovered("mc_track_1", 3, "album_hot_fuss", "Hot Fuss"),
            ProviderResolved("mc_track_1", 4, ProviderName.Spotify, "spotify-1"));

        var track = await env.LoadTrackAsync("mc_track_1");
        var artist = await env.LoadArtistAsync("artist_the_killers");
        var album = await env.LoadAlbumAsync("album_hot_fuss");
        var search = await env.SearchAsync("mr brightside", types: "track", playback: "spotify");

        track.Should().NotBeNull();
        track!.ArtistId.Should().Be("artist_the_killers");
        track.AlbumId.Should().Be("album_hot_fuss");
        track.Title.Should().Be("Mr. Brightside");
        track.AvailableProviders.Should().ContainSingle().Which.Should().Be(ProviderName.Spotify.Value);

        artist.Should().NotBeNull();
        artist!.Name.Should().Be("The Killers");
        artist.MusicBrainzArtistId.Should().Be("mb-artist-the-killers");
        artist.AvailableProviders.Should().Contain(ProviderName.Spotify.Value);

        album.Should().NotBeNull();
        album!.ArtistId.Should().Be("artist_the_killers");
        album.Name.Should().Be("Hot Fuss");
        album.MusicBrainzReleaseId.Should().Be("mb-release-hot-fuss");
        album.AvailableProviders.Should().Contain(ProviderName.Spotify.Value);

        search.Results.Should().ContainSingle();
        search.Results[0].Id.Should().Be("mc_track_1");
        search.Results[0].PlayabilityStatus.Should().Be(PlayabilityStatus.Playable);
        search.Results[0].AvailableProviders.Should().ContainSingle().Which.Should().Be(ProviderName.Spotify);
    }

    [Fact]
    public async Task Given_Persisted_MusicTrack_Stored_Events_When_Replaying_Then_Catalog_Documents_Are_Rebuilt_From_The_Event_Stream()
    {
        await using var env = RavenCatalogProjectionReplayTestEnvironment.Create();

        await env.AppendStoredEventsAsync(
            MinimalTrackInfo("mc_track_1", 1, "Mr. Brightside", "The Killers"),
            ArtistDiscovered("mc_track_1", 2, "artist_the_killers", "The Killers"),
            AlbumDiscovered("mc_track_1", 3, "album_hot_fuss", "Hot Fuss"),
            ProviderResolved("mc_track_1", 4, ProviderName.Spotify, "spotify-1"));

        var replayedEventCount = await env.ReplayAsync(MusicCatalogId.From("mc_track_1"));
        var track = await env.LoadTrackAsync("mc_track_1");
        var artist = await env.LoadArtistAsync("artist_the_killers");
        var album = await env.LoadAlbumAsync("album_hot_fuss");
        var search = await env.SearchAsync("mr brightside", types: "track", playback: "spotify");

        replayedEventCount.Should().Be(4);

        track.Should().NotBeNull();
        track!.ArtistId.Should().Be("artist_the_killers");
        track.AlbumId.Should().Be("album_hot_fuss");
        track.AvailableProviders.Should().Contain(ProviderName.Spotify.Value);

        artist.Should().NotBeNull();
        artist!.MusicBrainzArtistId.Should().Be("mb-artist-the-killers");
        artist!.AvailableProviders.Should().Contain(ProviderName.Spotify.Value);

        album.Should().NotBeNull();
        album!.MusicBrainzReleaseId.Should().Be("mb-release-hot-fuss");
        album!.AvailableProviders.Should().Contain(ProviderName.Spotify.Value);

        search.Results.Should().ContainSingle();
        search.Results[0].Id.Should().Be("mc_track_1");
    }

    [Fact]
    public async Task Given_Persisted_MusicTrack_Stored_Events_For_Multiple_Streams_When_Replaying_One_Stream_Then_Only_That_Stream_Is_Rebuilt()
    {
        await using var env = RavenCatalogProjectionReplayTestEnvironment.Create();

        await env.AppendStoredEventsAsync(
            MinimalTrackInfo("mc_track_1", 1, "Mr. Brightside", "The Killers"),
            ArtistDiscovered("mc_track_1", 2, "artist_the_killers", "The Killers"),
            MinimalTrackInfo("mc_track_2", 1, "Smile Like You Mean It", "The Killers"),
            ArtistDiscovered("mc_track_2", 2, "artist_the_killers", "The Killers"));

        var replayedEventCount = await env.ReplayAsync(MusicCatalogId.From("mc_track_1"));
        var firstTrack = await env.LoadTrackAsync("mc_track_1");
        var secondTrack = await env.LoadTrackAsync("mc_track_2");

        replayedEventCount.Should().Be(2);
        firstTrack.Should().NotBeNull();
        secondTrack.Should().BeNull();
    }

    [Fact]
    public async Task Given_Album_Linked_Before_Artist_Linked_When_Projecting_Then_The_Album_Hierarchy_Is_Repaired_When_Artist_Arrives()
    {
        await using var env = RavenCatalogProjectionReplayTestEnvironment.Create();

        await env.ApplyAsync(
            MinimalTrackInfo("mc_track_1", 1, "Mr. Brightside", "The Killers"),
            AlbumDiscovered("mc_track_1", 2, "album_hot_fuss", "Hot Fuss"),
            ArtistDiscovered("mc_track_1", 3, "artist_the_killers", "The Killers"));

        var album = await env.LoadAlbumAsync("album_hot_fuss");
        var track = await env.LoadTrackAsync("mc_track_1");

        album.Should().NotBeNull();
        album!.ArtistId.Should().Be("artist_the_killers");
        album.ArtistName.Should().Be("The Killers");

        track.Should().NotBeNull();
        track!.ArtistId.Should().Be("artist_the_killers");
        track.AlbumId.Should().Be("album_hot_fuss");
    }

    [Fact]
    public async Task Given_A_Duplicate_Stored_Event_When_Projecting_Then_It_Is_Ignored()
    {
        await using var env = RavenCatalogProjectionReplayTestEnvironment.Create();
        var storedEvent = ProviderResolved("mc_track_1", 1, ProviderName.Spotify, "spotify-1");

        await env.ApplyAsync(
            MinimalTrackInfo("mc_track_1", 0, "Mr. Brightside", "The Killers"),
            storedEvent,
            storedEvent);

        var track = await env.LoadTrackAsync("mc_track_1");

        track.Should().NotBeNull();
        track!.AvailableProviders.Should().ContainSingle().Which.Should().Be(ProviderName.Spotify.Value);
    }

    [Fact]
    public async Task Given_An_Out_Of_Order_Batch_When_Projecting_Then_The_Handler_Orders_Events_Before_Applying_Them()
    {
        await using var env = RavenCatalogProjectionReplayTestEnvironment.Create();

        await env.ApplyAsync(
            ProviderResolved("mc_track_1", 4, ProviderName.Spotify, "spotify-1"),
            AlbumDiscovered("mc_track_1", 3, "album_hot_fuss", "Hot Fuss"),
            MinimalTrackInfo("mc_track_1", 1, "Mr. Brightside", "The Killers"),
            ArtistDiscovered("mc_track_1", 2, "artist_the_killers", "The Killers"));

        var track = await env.LoadTrackAsync("mc_track_1");
        var artist = await env.LoadArtistAsync("artist_the_killers");
        var album = await env.LoadAlbumAsync("album_hot_fuss");

        track.Should().NotBeNull();
        track!.ArtistId.Should().Be("artist_the_killers");
        track.AlbumId.Should().Be("album_hot_fuss");
        track.AvailableProviders.Should().Contain(ProviderName.Spotify.Value);

        artist.Should().NotBeNull();
        album.Should().NotBeNull();
    }

    [Fact]
    public async Task Given_A_Provider_Failure_Event_When_Projecting_Then_The_Provider_Becomes_Terminally_Unavailable()
    {
        await using var env = RavenCatalogProjectionReplayTestEnvironment.Create();

        await env.ApplyAsync(
            MinimalTrackInfo("mc_track_1", 1, "Mr. Brightside", "The Killers"),
            ArtistDiscovered("mc_track_1", 2, "artist_the_killers", "The Killers"),
            AlbumDiscovered("mc_track_1", 3, "album_hot_fuss", "Hot Fuss"),
            ProviderFailed("mc_track_1", 4, ProviderName.YoutubeMusic));

        var track = await env.LoadTrackAsync("mc_track_1");
        var search = await env.SearchAsync("mr brightside", types: "track");

        track.Should().NotBeNull();
        track!.TerminallyUnavailableProviders.Should().ContainSingle().Which.Should().Be(ProviderName.YoutubeMusic.Value);
        search.Results.Should().ContainSingle();
        search.Results[0].PlayabilityStatus.Should().Be(PlayabilityStatus.TerminallyUnavailable);
        search.Results[0].TerminallyUnavailableProviders.Should().ContainSingle().Which.Should().Be(ProviderName.YoutubeMusic);
    }

    [Fact]
    public async Task Given_Artwork_Events_For_Track_Artist_And_Album_When_Projecting_Then_All_Catalog_Documents_Are_Updated()
    {
        await using var env = RavenCatalogProjectionReplayTestEnvironment.Create();

        await env.ApplyAsync(
            MinimalTrackInfo("mc_track_1", 1, "Mr. Brightside", "The Killers"),
            ArtistDiscovered("mc_track_1", 2, "artist_the_killers", "The Killers"),
            AlbumDiscovered("mc_track_1", 3, "album_hot_fuss", "Hot Fuss"),
            ArtworkDiscovered("mc_track_1", 4, "Track", null, "https://images.example.com/track.png"),
            ArtworkDiscovered("mc_track_1", 5, "Artist", "artist_the_killers", "https://images.example.com/artist.png"),
            ArtworkDiscovered("mc_track_1", 6, "Album", "album_hot_fuss", "https://images.example.com/album.png"));

        var track = await env.LoadTrackAsync("mc_track_1");
        var artist = await env.LoadArtistAsync("artist_the_killers");
        var album = await env.LoadAlbumAsync("album_hot_fuss");

        track.Should().NotBeNull();
        track!.ArtworkUrl.Should().Be("https://images.example.com/track.png");

        artist.Should().NotBeNull();
        artist!.ArtworkUrl.Should().Be("https://images.example.com/artist.png");

        album.Should().NotBeNull();
        album!.ArtworkUrl.Should().Be("https://images.example.com/album.png");
    }

    [Fact]
    public async Task Given_A_Metadata_Correction_Event_When_Projecting_Then_The_Catalog_Hierarchy_And_Search_Data_Are_Repaired()
    {
        await using var env = RavenCatalogProjectionReplayTestEnvironment.Create();

        await env.ApplyAsync(
            MinimalTrackInfo("mc_track_1", 1, "Mr Brightside", "Killers"),
            MetadataCorrected(
                "mc_track_1",
                2,
                "Mr. Brightside",
                "The Killers",
                "artist_the_killers",
                "Hot Fuss",
                "album_hot_fuss"));

        var track = await env.LoadTrackAsync("mc_track_1");
        var artist = await env.LoadArtistAsync("artist_the_killers");
        var album = await env.LoadAlbumAsync("album_hot_fuss");
        var search = await env.SearchAsync("mr brightside", types: "track");

        track.Should().NotBeNull();
        track!.Title.Should().Be("Mr. Brightside");
        track.ArtistName.Should().Be("The Killers");
        track.ArtistId.Should().Be("artist_the_killers");
        track.AlbumId.Should().Be("album_hot_fuss");
        track.AlbumName.Should().Be("Hot Fuss");

        artist.Should().NotBeNull();
        artist!.Name.Should().Be("The Killers");
        artist.MusicBrainzArtistId.Should().Be("mb-artist-the-killers");

        album.Should().NotBeNull();
        album!.Name.Should().Be("Hot Fuss");
        album.ArtistId.Should().Be("artist_the_killers");
        album.MusicBrainzReleaseId.Should().Be("mb-release-hot-fuss");
        album.ReleaseDate.Should().Be(new DateOnly(2004, 6, 7));

        search.Results.Should().ContainSingle();
        search.Results[0].Name.Should().Be("Mr. Brightside");
        search.Results[0].ArtistName.Should().Be("The Killers");
    }

    [Fact]
    public async Task Given_Provider_Resolution_Before_Hierarchy_When_Projecting_Then_Artist_And_Album_Providers_Are_Repaired_When_Hierarchy_Arrives()
    {
        await using var env = RavenCatalogProjectionReplayTestEnvironment.Create();

        await env.ApplyAsync(
            MinimalTrackInfo("mc_track_1", 1, "Mr. Brightside", "The Killers"),
            ProviderResolved("mc_track_1", 2, ProviderName.Spotify, "spotify-1"),
            ArtistDiscovered("mc_track_1", 3, "artist_the_killers", "The Killers"),
            AlbumDiscovered("mc_track_1", 4, "album_hot_fuss", "Hot Fuss"));

        var artist = await env.LoadArtistAsync("artist_the_killers");
        var album = await env.LoadAlbumAsync("album_hot_fuss");

        artist.Should().NotBeNull();
        artist!.AvailableProviders.Should().Contain(ProviderName.Spotify.Value);

        album.Should().NotBeNull();
        album!.AvailableProviders.Should().Contain(ProviderName.Spotify.Value);
    }

    [Fact]
    public async Task Given_Provider_Failure_Before_Hierarchy_When_Projecting_Then_Artist_And_Album_Failures_Are_Repaired_When_Hierarchy_Arrives()
    {
        await using var env = RavenCatalogProjectionReplayTestEnvironment.Create();

        await env.ApplyAsync(
            MinimalTrackInfo("mc_track_1", 1, "Mr. Brightside", "The Killers"),
            ProviderFailed("mc_track_1", 2, ProviderName.YoutubeMusic),
            ArtistDiscovered("mc_track_1", 3, "artist_the_killers", "The Killers"),
            AlbumDiscovered("mc_track_1", 4, "album_hot_fuss", "Hot Fuss"));

        var artist = await env.LoadArtistAsync("artist_the_killers");
        var album = await env.LoadAlbumAsync("album_hot_fuss");

        artist.Should().NotBeNull();
        artist!.TerminallyUnavailableProviders.Should().Contain(ProviderName.YoutubeMusic.Value);

        album.Should().NotBeNull();
        album!.TerminallyUnavailableProviders.Should().Contain(ProviderName.YoutubeMusic.Value);
    }

    [Fact]
    public async Task Given_Track_Artwork_Before_Hierarchy_When_Projecting_Then_Artist_And_Album_Artwork_Are_Repaired_When_Hierarchy_Arrives()
    {
        await using var env = RavenCatalogProjectionReplayTestEnvironment.Create();

        await env.ApplyAsync(
            MinimalTrackInfo("mc_track_1", 1, "Mr. Brightside", "The Killers"),
            ArtworkDiscovered("mc_track_1", 2, "Track", null, "https://images.example.com/track.png"),
            ArtistDiscovered("mc_track_1", 3, "artist_the_killers", "The Killers"),
            AlbumDiscovered("mc_track_1", 4, "album_hot_fuss", "Hot Fuss"));

        var artist = await env.LoadArtistAsync("artist_the_killers");
        var album = await env.LoadAlbumAsync("album_hot_fuss");

        artist.Should().NotBeNull();
        artist!.ArtworkUrl.Should().Be("https://images.example.com/track.png");

        album.Should().NotBeNull();
        album!.ArtworkUrl.Should().Be("https://images.example.com/track.png");
    }

    private static MusicTrackStoredEventRecordDto MinimalTrackInfo(
        string musicCatalogId,
        int version,
        string title,
        string artist) =>
        Translator.ToDto(
            MusicCatalogId.From(musicCatalogId),
            version,
            CommandId.For($"CatalogReplay:{musicCatalogId}:{version}"),
            new TrackDiscovered(
                title,
                artist,
                222000,
                "USIR20400274",
                "mbid-1",
                ProviderName.MusicBrainz,
                new DateTimeOffset(2026, 6, 15, 12, 0, 0, TimeSpan.Zero)));

    private static MusicTrackStoredEventRecordDto ArtistDiscovered(
        string musicCatalogId,
        int version,
        string artistId,
        string artistName) =>
        Translator.ToDto(
            MusicCatalogId.From(musicCatalogId),
            version,
            CommandId.For($"CatalogReplay:{musicCatalogId}:{version}"),
            new ArtistDiscovered(
                artistId,
                artistName,
                "mb-artist-the-killers",
                ProviderName.MusicBrainz,
                new DateTimeOffset(2026, 6, 15, 12, 1, 0, TimeSpan.Zero)));

    private static MusicTrackStoredEventRecordDto AlbumDiscovered(
        string musicCatalogId,
        int version,
        string albumId,
        string albumName) =>
        Translator.ToDto(
            MusicCatalogId.From(musicCatalogId),
            version,
            CommandId.For($"CatalogReplay:{musicCatalogId}:{version}"),
            new AlbumDiscovered(
                albumId,
                albumName,
                "mb-release-hot-fuss",
                new DateOnly(2004, 6, 7),
                ProviderName.MusicBrainz,
                new DateTimeOffset(2026, 6, 15, 12, 2, 0, TimeSpan.Zero)));

    private static MusicTrackStoredEventRecordDto ProviderResolved(
        string musicCatalogId,
        int version,
        ProviderName provider,
        string externalId) =>
        Translator.ToDto(
            MusicCatalogId.From(musicCatalogId),
            version,
            CommandId.For($"CatalogReplay:{musicCatalogId}:{version}"),
            new ProviderReferenceDiscovered(
                provider,
                externalId,
                new Uri($"https://example.com/{externalId}"),
                ProviderName.Odesli,
                new DateTimeOffset(2026, 6, 15, 12, 3, 0, TimeSpan.Zero)));

    private static MusicTrackStoredEventRecordDto ProviderFailed(
        string musicCatalogId,
        int version,
        ProviderName provider) =>
        Translator.ToDto(
            MusicCatalogId.From(musicCatalogId),
            version,
            CommandId.For($"CatalogReplay:{musicCatalogId}:{version}"),
            new ProviderReferenceLookupFailed(
                provider,
                ProviderName.Odesli,
                new DateTimeOffset(2026, 6, 15, 12, 4, 0, TimeSpan.Zero)));

    private static MusicTrackStoredEventRecordDto ArtworkDiscovered(
        string musicCatalogId,
        int version,
        string entityKind,
        string? entityId,
        string url) =>
        Translator.ToDto(
            MusicCatalogId.From(musicCatalogId),
            version,
            CommandId.For($"CatalogReplay:{musicCatalogId}:{version}"),
            new ArtworkDiscovered(
                Enum.Parse<CatalogEntityKind>(entityKind, ignoreCase: true),
                entityId,
                new Uri(url),
                "worker/musicbrainz",
                new DateTimeOffset(2026, 6, 15, 12, version, 0, TimeSpan.Zero)));

    private static MusicTrackStoredEventRecordDto MetadataCorrected(
        string musicCatalogId,
        int version,
        string title,
        string artistName,
        string artistId,
        string albumTitle,
        string albumId) =>
        Translator.ToDto(
            MusicCatalogId.From(musicCatalogId),
            version,
            CommandId.For($"CatalogReplay:{musicCatalogId}:{version}"),
            new MetadataCorrected(
                title,
                artistName,
                artistId,
                "mb-artist-the-killers",
                albumTitle,
                albumId,
                "mb-release-hot-fuss",
                new DateOnly(2004, 6, 7),
                222000,
                "USIR20400274",
                "mbid-1",
                "admin/repair",
                new DateTimeOffset(2026, 6, 15, 12, 5, 0, TimeSpan.Zero)));
}
