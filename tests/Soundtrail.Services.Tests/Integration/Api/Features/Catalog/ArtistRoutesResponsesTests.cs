using FluentAssertions;
using Soundtrail.Services.Tests.Integration.Api.Features.Search;
using System.Net;
using System.Net.Http.Json;

namespace Soundtrail.Services.Tests.Integration.Api.Features.Catalog;

public sealed class ArtistRoutesResponsesTests
{
    [Fact]
    public async Task Given_A_Known_Artist_When_Getting_The_Artist_Then_Its_Metadata_And_Albums_Are_Returned()
    {
        await using var factory = new CatalogRoutesApiFactory();
        factory.CatalogReadPort.Artist = ApiKnownTracks.TheKillersArtistDetails();

        var response = await factory.CreateClient().GetFromJsonAsync<ArtistResponse>("/artists/artist_the_killers");

        response!.Name.Should().Be("The Killers");
        response.Albums.Should().ContainSingle();
    }

    [Fact]
    public async Task Given_An_Unknown_Artist_When_Getting_The_Artist_Then_NotFound_Is_Returned()
    {
        await using var factory = new CatalogRoutesApiFactory();

        var response = await factory.CreateClient().GetAsync("/artists/artist_missing");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Given_A_Known_Artist_When_Getting_Artist_Tracks_Then_Tracks_Are_Returned()
    {
        await using var factory = new CatalogRoutesApiFactory();
        factory.CatalogReadPort.Artist = ApiKnownTracks.TheKillersArtistDetails();
        factory.CatalogReadPort.ArtistTracks = [ApiKnownTracks.MrBrightsideTrackSummary()];

        var response = await factory.CreateClient().GetFromJsonAsync<ArtistTracksResponse>("/artists/artist_the_killers/tracks");

        response!.Tracks.Should().ContainSingle();
        response.Tracks[0].Title.Should().Be("Mr. Brightside");
    }

    private sealed class ArtistResponse
    {
        public string Name { get; set; } = string.Empty;
        public List<AlbumResponse> Albums { get; set; } = [];
    }

    private sealed class AlbumResponse
    {
        public string Name { get; set; } = string.Empty;
    }

    private sealed class ArtistTracksResponse
    {
        public List<TrackResponse> Tracks { get; set; } = [];
    }

    private sealed class TrackResponse
    {
        public string Title { get; set; } = string.Empty;
    }
}
