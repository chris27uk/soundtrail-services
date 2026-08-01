using Soundtrail.Domain.Catalog.Albums;
using Soundtrail.Domain.Common;
using Soundtrail.Services.Api.Features.Catalog.Shared.Contract;

namespace Soundtrail.Services.Tests.Unit.GetTracksForAlbum;

public sealed class AlbumTracksDoNotExistTests
{
    [Fact]
    public async Task Given_Missing_Album_Tracks_When_Requesting_The_Album_Tracks_Then_No_Album_Tracks_Are_Returned()
    {
        var environment = GetTracksForAlbumMissingUnitTestEnvironment.ForMissingAlbumTracks();

        var result = await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        result.Should().BeNull();
    }

    [Fact]
    public async Task Given_Missing_Album_Tracks_When_Requesting_The_Album_Tracks_Then_The_Requested_Album_Id_Is_Read()
    {
        var albumId = AlbumId.From("artist-1407", "album-1507");
        var environment = GetTracksForAlbumMissingUnitTestEnvironment.ForMissingAlbumTracks(albumId);

        await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        environment.Port.RequestedAlbumIds.Single().Should().Be(albumId);
    }

    [Fact]
    public async Task Given_Missing_Album_Tracks_With_Discovery_Feedback_When_Requesting_The_Album_Tracks_Then_An_Empty_Response_With_Timing_Is_Returned()
    {
        var albumId = AlbumId.From("artist-1407", "album-1507");
        var environment = GetTracksForAlbumMissingUnitTestEnvironment.ForMissingAlbumTracks(albumId);
        environment.DiscoveryFeedbackPort.Response = new DiscoveryFeedbackResponse(
            "pending",
            LookupPriorityBand.High,
            environment.Clock.UtcNow.AddSeconds(15),
            environment.Clock.UtcNow.AddSeconds(75),
            "Album track lookup queued.",
            environment.Clock.UtcNow);

        var result = await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        result.Should().NotBeNull();
        result!.AlbumId.Should().Be(albumId);
        result.Tracks.Should().BeEmpty();
        result.Discovery.Should().Be(environment.DiscoveryFeedbackPort.Response);
    }
}
