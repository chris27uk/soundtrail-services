using Soundtrail.Domain.Catalog.Playlists;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForPlaylist.Contract;
using Soundtrail.Services.Api.Features.Catalog.Shared.Contract;

namespace Soundtrail.Services.Tests.Unit.GetTracksForPlaylist;

public sealed class PlaylistTracksExistTests
{
    [Fact]
    public async Task Given_Existing_Playlist_Tracks_When_Requesting_Then_The_Requested_Playlist_Is_Read()
    {
        var playlistId = PlaylistId.FromPlaylistName("FocusedMix");
        var environment = GetTracksForPlaylistUnitTestEnvironment.ForExistingPlaylistTracks(playlistId: playlistId);
        var sut = environment.CreateSubjectUnderTest();

        await sut.Handle(environment.CreateRequest());

        environment.Port.RequestedPlaylistIds.Single().Should().Be(playlistId);
    }

    [Fact]
    public async Task Given_Existing_Playlist_Tracks_When_Requesting_Then_High_Priority_Discovery_Is_Requested()
    {
        var environment = GetTracksForPlaylistUnitTestEnvironment.ForExistingPlaylistTracks();
        var sut = environment.CreateSubjectUnderTest();

        await sut.Handle(environment.CreateRequest());

        environment.SentMessage<RequestKnownMusicDataMessage>().Priority.Should().Be(LookupPriorityBand.High);
    }

    [Fact]
    public async Task Given_Existing_Playlist_Tracks_When_Requesting_Then_The_Request_Uses_Full_Trust()
    {
        var environment = GetTracksForPlaylistUnitTestEnvironment.ForExistingPlaylistTracks();
        var sut = environment.CreateSubjectUnderTest();

        await sut.Handle(environment.CreateRequest());

        environment.SentMessage<RequestKnownMusicDataMessage>().TrustLevel.Should().Be(100);
    }

    [Fact]
    public async Task Given_Existing_Playlist_Tracks_When_Requesting_Then_The_Request_Has_No_Risk()
    {
        var environment = GetTracksForPlaylistUnitTestEnvironment.ForExistingPlaylistTracks();
        var sut = environment.CreateSubjectUnderTest();

        await sut.Handle(environment.CreateRequest());

        environment.SentMessage<RequestKnownMusicDataMessage>().RiskScore.Should().Be(0);
    }

    [Fact]
    public async Task Given_A_Completed_Playlist_Projection_With_No_Tracks_When_Requesting_Then_Catch_Up_Is_Scheduled()
    {
        var response = new GetTracksForPlaylistResponse(
            PlaylistTracks.DefaultPlaylistId,
            [],
            new DiscoveryFeedbackResponse(
                "completed",
                LookupPriorityBand.High,
                null,
                null,
                "Lookup completed.",
                new DateTimeOffset(2024, 6, 7, 8, 9, 9, TimeSpan.Zero)));
        var environment = GetTracksForPlaylistUnitTestEnvironment.ForExistingPlaylistTracks(response: response);
        var sut = environment.CreateSubjectUnderTest();

        var result = await sut.Handle(environment.CreateRequest());

        result!.Discovery!.Reason.Should().Be("Playlist projection is still catching up.");
    }

    [Fact]
    public async Task Given_A_Completed_Playlist_With_An_Unplayable_Track_When_Requesting_Then_Discovery_Remains_Completed()
    {
        var response = PlaylistTracks.CreateResponse(
            discovery: new DiscoveryFeedbackResponse(
                "completed",
                LookupPriorityBand.High,
                null,
                null,
                "Lookup completed.",
                new DateTimeOffset(2024, 6, 7, 8, 9, 8, TimeSpan.Zero)));
        var environment = GetTracksForPlaylistUnitTestEnvironment.ForExistingPlaylistTracks(response: response);
        var sut = environment.CreateSubjectUnderTest();

        var result = await sut.Handle(environment.CreateRequest());

        result!.Discovery!.Status.Should().Be("completed");
    }
}
