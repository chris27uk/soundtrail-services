using FluentAssertions;
using Soundtrail.Services.Features.CatalogLookup;
using Soundtrail.Services.Features.CatalogLookup.Contracts;
using Soundtrail.Services.Features.CatalogLookup.Models;
using Soundtrail.Services.Features.Tracks;

namespace Soundtrail.Services.Tests.Unit.Features.CatalogLookup;

public sealed class CatalogLookupHandlerTests
{
    [Fact]
    public async Task Given_A_Known_Identifier_When_The_Handler_Is_Called_Then_It_Returns_The_Matching_Track()
    {
        var track = new Track(
            TrackTitle.From("Mr. Brightside"),
            ArtistName.From("The Killers"),
            Isrc.From("USIR20400274"),
            Mbid.From("mr-brightside-mbid"),
            AppleId.From("apple-mr-brightside"),
            SpotifyId.From("spotify-mr-brightside"),
            DurationMs.From(222000));
        var port = new StubCatalogLookupPort(track);
        var sut = new CatalogLookupHandler(port);

        var result = await sut.Handle(CatalogLookupRequest.ByIsrc("USIR20400274"));

        result.Should().BeEquivalentTo(track);
    }

    private sealed class StubCatalogLookupPort(Track? track) : ICatalogLookupPort
    {
        public Task<Track?> LookupAsync(CatalogLookupRequest request, CancellationToken cancellationToken) =>
            Task.FromResult(track);

        public Task<bool> IsReadyAsync(CancellationToken cancellationToken) => Task.FromResult(true);
    }
}
