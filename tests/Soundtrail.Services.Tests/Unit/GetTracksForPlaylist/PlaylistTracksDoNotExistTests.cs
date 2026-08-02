using Soundtrail.Domain.Catalog.Playlists;
using Soundtrail.Domain.Common;
using Soundtrail.Services.Api.Features.Catalog.Shared.Contract;

namespace Soundtrail.Services.Tests.Unit.GetTracksForPlaylist;

public sealed class PlaylistTracksDoNotExistTests
{
    [Fact]
    public async Task Given_Missing_Playlist_Tracks_When_Requesting_The_Playlist_Tracks_Then_No_Playlist_Tracks_Are_Returned()
    {
        var playlistId = PlaylistId.FromPlaylistName("UnknownPlaylist");
        var environment = GetTracksForPlaylistMissingUnitTestEnvironment.ForMissingPlaylistTracks(playlistId);

        var result = await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        result.Should().NotBeNull();
        result!.PlaylistId.Should().Be(playlistId);
        result.Tracks.Should().BeEmpty();
        result.Discovery.Should().NotBeNull();
        result.Discovery!.Status.Should().Be("scheduled");
        result.Discovery.Priority.Should().Be(LookupPriorityBand.High);
        result.Discovery.NextEligibleAt.Should().Be(environment.Clock.UtcNow.AddSeconds(15));
        result.Discovery.EarliestExpectedCompletionAt.Should().Be(environment.Clock.UtcNow.AddSeconds(75));
        result.Discovery.Reason.Should().Be("Playlist lookup queued.");
        result.Discovery.UpdatedAtUtc.Should().Be(environment.Clock.UtcNow);
    }

    [Fact]
    public async Task Given_Missing_Playlist_Tracks_With_Discovery_Feedback_When_Requesting_The_Playlist_Tracks_Then_An_Empty_Response_With_Timing_Is_Returned()
    {
        var playlistId = PlaylistId.FromPlaylistName("UnknownPlaylist");
        var environment = GetTracksForPlaylistMissingUnitTestEnvironment.ForMissingPlaylistTracks(playlistId);
        environment.DiscoveryFeedbackPort.Response = new DiscoveryFeedbackResponse(
            "pending",
            LookupPriorityBand.High,
            environment.Clock.UtcNow.AddSeconds(15),
            environment.Clock.UtcNow.AddSeconds(75),
            "Playlist lookup queued.",
            environment.Clock.UtcNow);

        var result = await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        result.Should().NotBeNull();
        result!.PlaylistId.Should().Be(playlistId);
        result.Tracks.Should().BeEmpty();
        result.Discovery.Should().Be(environment.DiscoveryFeedbackPort.Response);
    }

    [Fact]
    public async Task Given_Missing_Playlist_Tracks_With_Completed_Discovery_When_Requesting_Then_Projection_Catch_Up_Timing_Is_Returned()
    {
        var playlistId = PlaylistId.FromPlaylistName("UnknownPlaylist");
        var environment = GetTracksForPlaylistMissingUnitTestEnvironment.ForMissingPlaylistTracks(playlistId);
        environment.DiscoveryFeedbackPort.Response = new DiscoveryFeedbackResponse(
            "completed",
            LookupPriorityBand.High,
            null,
            null,
            "Lookup completed.",
            environment.Clock.UtcNow.AddSeconds(-1));

        var result = await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        result.Should().NotBeNull();
        result!.Tracks.Should().BeEmpty();
        result.Discovery.Should().NotBeNull();
        result.Discovery!.Status.Should().Be("scheduled");
        result.Discovery.NextEligibleAt.Should().Be(environment.Clock.UtcNow.AddSeconds(15));
        result.Discovery.EarliestExpectedCompletionAt.Should().Be(environment.Clock.UtcNow.AddSeconds(75));
        result.Discovery.Reason.Should().Be("Playlist projection is still catching up.");
    }
}
