using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Common;
using Soundtrail.Services.Api.Features.Catalog.Shared.Contract;

namespace Soundtrail.Services.Tests.Unit.GetAlbumsForArtist;

public sealed class ArtistAlbumsDoNotExistTests
{
    [Fact]
    public async Task Given_Missing_Artist_Albums_When_Requesting_The_Artist_Albums_Then_No_Artist_Albums_Are_Returned()
    {
        var environment = GetAlbumsForArtistMissingUnitTestEnvironment.ForMissingArtistAlbums();

        var result = await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        result.Should().BeNull();
    }

    [Fact]
    public async Task Given_Missing_Artist_Albums_When_Requesting_The_Artist_Albums_Then_The_Requested_Artist_Id_Is_Read()
    {
        var artistId = ArtistId.From("artist-1707");
        var environment = GetAlbumsForArtistMissingUnitTestEnvironment.ForMissingArtistAlbums(artistId);

        await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        environment.Port.RequestedArtistIds.Single().Should().Be(artistId);
    }

    [Fact]
    public async Task Given_Missing_Artist_Albums_With_Discovery_Feedback_When_Requesting_The_Artist_Albums_Then_An_Empty_Response_With_Timing_Is_Returned()
    {
        var artistId = ArtistId.From("artist-1707");
        var environment = GetAlbumsForArtistMissingUnitTestEnvironment.ForMissingArtistAlbums(artistId);
        environment.DiscoveryFeedbackPort.Response = new DiscoveryFeedbackResponse(
            "pending",
            LookupPriorityBand.High,
            environment.Clock.UtcNow.AddSeconds(15),
            environment.Clock.UtcNow.AddSeconds(75),
            "Artist album lookup queued.",
            environment.Clock.UtcNow);

        var result = await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        result.Should().NotBeNull();
        result!.ArtistId.Should().Be(artistId);
        result.Albums.Should().BeEmpty();
        result.Discovery.Should().Be(environment.DiscoveryFeedbackPort.Response);
    }
}
