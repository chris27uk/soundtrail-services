using Soundtrail.Domain.Common;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForPlaylist.Contract;

namespace Soundtrail.Services.Tests.Unit.Sociable.Features.GetTracksForPlaylist.Scenarios.NoExistingDataOrRequests.Api;

public sealed class NoExistingDataOrRequestsTests
{
    [Fact]
    public async Task When_Requesting_Then_No_Tracks_Are_Returned()
    {
        var environment = GetTracksForPlaylistSociableTestEnvironment.ForNoExistingDataOrRequests();

        var response = await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForPlaylistRequest(environment.PlaylistId)));

        response!.Tracks.Should().BeEmpty();
    }

    [Fact]
    public async Task When_Requesting_Then_The_Playlist_Id_Is_Returned()
    {
        var environment = GetTracksForPlaylistSociableTestEnvironment.ForNoExistingDataOrRequests();

        var response = await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForPlaylistRequest(environment.PlaylistId)));

        response!.PlaylistId.Should().Be(environment.PlaylistId);
    }

    [Fact]
    public async Task When_Requesting_Then_Discovery_Is_Scheduled()
    {
        var environment = GetTracksForPlaylistSociableTestEnvironment.ForNoExistingDataOrRequests();

        var response = await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForPlaylistRequest(environment.PlaylistId)));

        response!.Discovery!.Status.Should().Be("scheduled");
    }

    [Fact]
    public async Task When_Requesting_Then_Discovery_Has_High_Priority()
    {
        var environment = GetTracksForPlaylistSociableTestEnvironment.ForNoExistingDataOrRequests();

        var response = await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForPlaylistRequest(environment.PlaylistId)));

        response!.Discovery!.Priority.Should().Be(LookupPriorityBand.High);
    }

    [Fact]
    public async Task When_Requesting_Then_The_Next_Attempt_Is_In_Fifteen_Seconds()
    {
        var requestTime = new DateTimeOffset(2026, 8, 2, 9, 46, 0, TimeSpan.Zero);
        var environment = GetTracksForPlaylistSociableTestEnvironment.ForNoExistingDataOrRequests(requestTime);

        var response = await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForPlaylistRequest(environment.PlaylistId)));

        response!.Discovery!.NextEligibleAt.Should().Be(requestTime.AddSeconds(15));
    }
}
