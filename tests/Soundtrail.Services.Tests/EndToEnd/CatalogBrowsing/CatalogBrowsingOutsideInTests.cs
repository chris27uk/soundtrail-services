using FluentAssertions;
using Soundtrail.Contracts.Common;
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
