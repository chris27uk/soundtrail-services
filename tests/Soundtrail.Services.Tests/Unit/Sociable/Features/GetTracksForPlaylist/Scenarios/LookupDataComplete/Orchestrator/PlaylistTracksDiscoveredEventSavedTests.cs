using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Domain.Discovery.Events;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForPlaylist.Contract;
using Soundtrail.Services.Tests.Unit.Sociable.Features.GetTracksForPlaylist.Support;

namespace Soundtrail.Services.Tests.Unit.Sociable.Features.GetTracksForPlaylist.Scenarios.LookupDataComplete.Orchestrator;

public sealed class PlaylistTracksDiscoveredEventSavedTests
{
    [Fact]
    public async Task Then_The_Playlist_Id_Is_Saved()
    {
        var environment = ForCompletedPlaylistTrack();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForPlaylistRequest(environment.PlaylistId)));

        environment.SavedEvent<PlaylistTracksDiscovered>().PlaylistId.Should().Be(environment.PlaylistId);
    }

    [Fact]
    public async Task Then_The_Input_Track_Is_Saved()
    {
        const string artist = "Playlist Event Artist";
        const string title = "Playlist Event Title";
        var expectedTrackId = TrackId.TryCreate(artist, title, null, null, null) switch
        {
            TrackIdCreateResult.Success success => success.Value.Value,
            _ => throw new InvalidOperationException("The test input must produce a track id.")
        };
        var environment = ForCompletedPlaylistTrack(artist, title);

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForPlaylistRequest(environment.PlaylistId)));

        environment.SavedEvent<PlaylistTracksDiscovered>().Tracks.Single().Value.Should().Be(expectedTrackId);
    }

    [Fact]
    public async Task Then_The_Observed_Time_Comes_From_The_Request()
    {
        var requestTime = new DateTimeOffset(2026, 8, 2, 11, 1, 0, TimeSpan.Zero);
        var environment = ForCompletedPlaylistTrack(requestTime: requestTime);

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForPlaylistRequest(environment.PlaylistId)));

        environment.SavedEvent<PlaylistTracksDiscovered>().ObservedAt.Should().Be(requestTime);
    }

    private static GetTracksForPlaylistSociableTestEnvironment ForCompletedPlaylistTrack(
        string artist = "Scenario Artist",
        string title = "Scenario Title",
        DateTimeOffset requestTime = default) =>
        GetTracksForPlaylistSociableTestEnvironment.ForLookupDataComplete(
            requestTime,
            LookupDataCompleteTrack.MatchingCatalogTrack(
                artist, title, artist, title, "Scenario Album", new DateOnly(2025, 2, 3), null, 120000, requestTime));
}
