using FluentAssertions;
using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Catalog.Events;
using Soundtrail.Services.Tests.Integration.Api.Infrastructure;
using System.Net;

namespace Soundtrail.Services.Tests.EndToEnd.CatalogBrowsing;

[Collection(RavenEmbeddedCollection.Name)]
public sealed class CatalogBrowsingOutsideInTests
{
    [Fact]
    public async Task Given_A_Known_Artist_When_Getting_The_Artist_Then_The_Artist_And_Its_Albums_Are_Returned()
    {
        await using var env = await CatalogBrowsingOutsideInTestEnvironment.CreateAsync(store =>
            CatalogBrowsingOutsideInTestEnvironment.SeedCatalog(
                store,
                MrBrightside(),
                SmileLikeYouMeanIt()));

        var response = await env.GetArtistAsync("artist_the_killers");

        response.Id.Should().Be("artist_the_killers");
        response.Name.Should().Be("The Killers");
        response.Albums.Select(x => x.Id).Should().BeEquivalentTo(["album_hot_fuss"]);
        response.Albums[0].PlayabilityStatus.Should().Be("Playable");
    }

    [Fact]
    public async Task Given_A_Known_Artist_When_Listing_Tracks_Then_Tracks_Across_Albums_Are_Returned()
    {
        await using var env = await CatalogBrowsingOutsideInTestEnvironment.CreateAsync(store =>
            CatalogBrowsingOutsideInTestEnvironment.SeedCatalog(
                store,
                MrBrightside(),
                Human()));

        var response = await env.ListTracksByArtistAsync("artist_the_killers");

        response.ArtistId.Should().Be("artist_the_killers");
        response.Tracks.Select(x => x.Id).Should().BeEquivalentTo(["track_human", "track_mr_brightside"], options => options.WithStrictOrdering());
    }

    [Fact]
    public async Task Given_A_Known_Album_When_Getting_The_Album_Then_The_Album_And_Its_Tracks_Are_Returned()
    {
        await using var env = await CatalogBrowsingOutsideInTestEnvironment.CreateAsync(store =>
            CatalogBrowsingOutsideInTestEnvironment.SeedCatalog(
                store,
                MrBrightside(),
                SmileLikeYouMeanIt()));

        var response = await env.GetAlbumAsync("artist_the_killers", "album_hot_fuss");

        response.Id.Should().Be("album_hot_fuss");
        response.Name.Should().Be("Hot Fuss");
        response.ReleaseDate.Should().Be(new DateOnly(2004, 6, 7));
        response.Tracks.Select(x => x.Id).Should().BeEquivalentTo(["track_mr_brightside", "track_smile_like_you_mean_it"], options => options.WithStrictOrdering());
    }

    [Fact]
    public async Task Given_A_Known_Album_When_Listing_Album_Tracks_Then_Only_That_Album_Tracks_Are_Returned()
    {
        await using var env = await CatalogBrowsingOutsideInTestEnvironment.CreateAsync(store =>
            CatalogBrowsingOutsideInTestEnvironment.SeedCatalog(
                store,
                MrBrightside(),
                SmileLikeYouMeanIt(),
                Human()));

        var response = await env.ListTracksByAlbumAsync("artist_the_killers", "album_hot_fuss");

        response.AlbumId.Should().Be("album_hot_fuss");
        response.Tracks.Select(x => x.Id).Should().BeEquivalentTo(["track_mr_brightside", "track_smile_like_you_mean_it"], options => options.WithStrictOrdering());
    }

    [Fact]
    public async Task Given_A_Known_Track_When_Getting_The_Track_Then_The_Track_Details_Are_Returned()
    {
        await using var env = await CatalogBrowsingOutsideInTestEnvironment.CreateAsync(store =>
            CatalogBrowsingOutsideInTestEnvironment.SeedCatalog(
                store,
                MrBrightside()));

        var response = await env.GetTrackAsync("artist_the_killers", "album_hot_fuss", "track_mr_brightside");

        response.Id.Should().Be("track_mr_brightside");
        response.Title.Should().Be("Mr. Brightside");
        response.Isrc.Should().Be("USIR20400274");
        response.PlayabilityStatus.Should().Be("Playable");
        response.AvailableProviders.Should().BeEquivalentTo(["spotify", "appleMusic"]);
        response.ProviderReferences.Should().ContainSingle();
        response.ProviderReferences[0].Provider.Should().Be("spotify");
        response.ProviderReferences[0].ProviderId.Should().Be("spotify-track-1");
    }

    [Fact]
    public async Task Given_A_Track_Requested_With_The_Wrong_Hierarchy_When_Getting_The_Track_Then_It_Returns_NotFound()
    {
        await using var env = await CatalogBrowsingOutsideInTestEnvironment.CreateAsync(store =>
            CatalogBrowsingOutsideInTestEnvironment.SeedCatalog(
                store,
                MrBrightside()));

        var response = await env.GetTrackRawAsync("artist_someone_else", "album_hot_fuss", "track_mr_brightside");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Given_Imported_Events_When_Catalog_Projection_Is_Rebuilt_Then_Catalog_Routes_Return_Rebuilt_Read_Models()
    {
        await using var env = await CatalogBrowsingOutsideInTestEnvironment.CreateAsync(store =>
            CatalogBrowsingOutsideInTestEnvironment.SeedRebuiltCatalogFromImportedEvents(
                store,
                MusicCatalogId.From("mc_track_1"),
                new TrackDiscovered("Mr. Brightside", "The Killers", 222000, "USIR20400274", "mbid-1", LookupSource.MusicBrainz, new DateTimeOffset(2026, 6, 16, 12, 0, 0, TimeSpan.Zero)),
                new ArtistDiscovered("artist_the_killers", "The Killers", "mb-artist-the-killers", LookupSource.MusicBrainz, new DateTimeOffset(2026, 6, 16, 12, 1, 0, TimeSpan.Zero)),
                new AlbumDiscovered("album_hot_fuss", "Hot Fuss", "mb-release-hot-fuss", new DateOnly(2004, 6, 7), LookupSource.MusicBrainz, new DateTimeOffset(2026, 6, 16, 12, 2, 0, TimeSpan.Zero)),
                new ProviderReferenceDiscovered(ProviderName.Spotify, "spotify-track-1", new Uri("https://open.spotify.com/track/spotify-track-1"), LookupSource.Odesli, new DateTimeOffset(2026, 6, 16, 12, 3, 0, TimeSpan.Zero))));

        var artist = await env.GetArtistAsync("artist_the_killers");
        var album = await env.GetAlbumAsync("artist_the_killers", "album_hot_fuss");
        var track = await env.GetTrackAsync("artist_the_killers", "album_hot_fuss", "mc_track_1");

        artist.Id.Should().Be("artist_the_killers");
        artist.Name.Should().Be("The Killers");
        artist.Albums.Select(x => x.Id).Should().Contain("album_hot_fuss");

        album.Id.Should().Be("album_hot_fuss");
        album.Name.Should().Be("Hot Fuss");
        album.ReleaseDate.Should().Be(new DateOnly(2004, 6, 7));
        album.Tracks.Select(x => x.Id).Should().Contain("mc_track_1");

        track.Id.Should().Be("mc_track_1");
        track.Title.Should().Be("Mr. Brightside");
        track.Isrc.Should().Be("USIR20400274");
        track.PlayabilityStatus.Should().Be("Playable");
        track.AvailableProviders.Should().Contain("spotify");
    }

    [Fact]
    public async Task Given_Imported_Metadata_Corrected_Events_When_Catalog_Projection_Is_Rebuilt_Then_Catalog_Routes_Return_Repaired_Read_Models()
    {
        await using var env = await CatalogBrowsingOutsideInTestEnvironment.CreateAsync(store =>
            CatalogBrowsingOutsideInTestEnvironment.SeedRebuiltCatalogFromImportedEvents(
                store,
                MusicCatalogId.From("mc_track_1"),
                new MetadataCorrected(
                    "Mr. Brightside",
                    "The Killers",
                    "artist_the_killers",
                    "mb-artist-the-killers",
                    "Hot Fuss",
                    "album_hot_fuss",
                    "mb-release-hot-fuss",
                    new DateOnly(2004, 6, 7),
                    222000,
                    "USIR20400274",
                    "mbid-1",
                    "admin/repair",
                    new DateTimeOffset(2026, 6, 16, 12, 0, 0, TimeSpan.Zero)),
                new ProviderReferenceDiscovered(
                    ProviderName.Spotify,
                    "spotify-track-1",
                    new Uri("https://open.spotify.com/track/spotify-track-1"),
                    LookupSource.Odesli,
                    new DateTimeOffset(2026, 6, 16, 12, 3, 0, TimeSpan.Zero))));

        var artist = await env.GetArtistAsync("artist_the_killers");
        var album = await env.GetAlbumAsync("artist_the_killers", "album_hot_fuss");
        var track = await env.GetTrackAsync("artist_the_killers", "album_hot_fuss", "mc_track_1");

        artist.Id.Should().Be("artist_the_killers");
        artist.Name.Should().Be("The Killers");
        artist.Albums.Select(x => x.Id).Should().Contain("album_hot_fuss");

        album.Id.Should().Be("album_hot_fuss");
        album.Name.Should().Be("Hot Fuss");
        album.ReleaseDate.Should().Be(new DateOnly(2004, 6, 7));
        album.Tracks.Select(x => x.Id).Should().Contain("mc_track_1");

        track.Id.Should().Be("mc_track_1");
        track.Title.Should().Be("Mr. Brightside");
        track.Isrc.Should().Be("USIR20400274");
        track.PlayabilityStatus.Should().Be("Playable");
        track.AvailableProviders.Should().Contain("spotify");
    }

    private static CatalogBrowsingOutsideInTestEnvironment.CatalogSeedTrack MrBrightside() =>
        new(
            "artist_the_killers",
            "The Killers",
            "album_hot_fuss",
            "Hot Fuss",
            "track_mr_brightside",
            "Mr. Brightside",
            "USIR20400274",
            222000,
            new DateOnly(2004, 6, 7),
            [ProviderName.Spotify, ProviderName.AppleMusic],
            [],
            [new CatalogBrowsingOutsideInTestEnvironment.ProviderReferenceContract
            {
                Provider = "spotify",
                ProviderEntityType = "track",
                ProviderId = "spotify-track-1",
                Url = new Uri("https://open.spotify.com/track/spotify-track-1"),
                DiscoveredAt = new DateTimeOffset(2026, 6, 17, 12, 0, 0, TimeSpan.Zero)
            }]);

    private static CatalogBrowsingOutsideInTestEnvironment.CatalogSeedTrack SmileLikeYouMeanIt() =>
        new(
            "artist_the_killers",
            "The Killers",
            "album_hot_fuss",
            "Hot Fuss",
            "track_smile_like_you_mean_it",
            "Smile Like You Mean It",
            "USIR20400275",
            234000,
            new DateOnly(2004, 6, 7),
            [ProviderName.Spotify],
            []);

    private static CatalogBrowsingOutsideInTestEnvironment.CatalogSeedTrack Human() =>
        new(
            "artist_the_killers",
            "The Killers",
            "album_day_and_age",
            "Day & Age",
            "track_human",
            "Human",
            "USIR20800001",
            245000,
            new DateOnly(2008, 11, 18),
            [],
            [ProviderName.YoutubeMusic]);
}
