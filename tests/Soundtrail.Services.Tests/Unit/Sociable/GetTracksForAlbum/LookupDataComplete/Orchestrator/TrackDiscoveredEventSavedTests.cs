using Soundtrail.Domain.Catalog.Events;
using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForAlbum.Contract;

namespace Soundtrail.Services.Tests.Unit.Sociable.GetTracksForAlbum.LookupDataComplete.Orchestrator;

public sealed class TrackDiscoveredEventSavedTests
{
    [Fact]
    public async Task Then_The_Input_Track_Is_Saved()
    {
        const string artist = "Album Event Artist";
        const string title = "Album Event Title";
        var expectedTrackId = TrackId.TryCreate(artist, title, "Album Event Album", new DateOnly(2025, 2, 3), null) switch
        {
            TrackIdCreateResult.Success success => success.Value.Value,
            _ => throw new InvalidOperationException("The test input must produce a track id.")
        };
        var environment = ForCompletedAlbumTrack(artist, title);

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForAlbumRequest(environment.AlbumId)));

        environment.SavedEvent<TrackDiscovered>().Track.TrackId.Value.Should().Be(expectedTrackId);
    }

    [Fact]
    public async Task Then_The_Track_Title_Is_Saved()
    {
        const string title = "Album Event Title";
        var environment = ForCompletedAlbumTrack(title: title);

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForAlbumRequest(environment.AlbumId)));

        environment.SavedEvent<TrackDiscovered>().Track.Title.Should().Be(title);
    }

    [Fact]
    public async Task Then_The_Observed_Time_Comes_From_The_Request()
    {
        var requestTime = new DateTimeOffset(2026, 8, 2, 11, 1, 0, TimeSpan.Zero);
        var environment = ForCompletedAlbumTrack(requestTime: requestTime);

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForAlbumRequest(environment.AlbumId)));

        environment.SavedEvent<TrackDiscovered>().ObservedAt.Should().Be(requestTime);
    }

    private static GetTracksForAlbumSociableTestEnvironment ForCompletedAlbumTrack(
        string artist = "Scenario Artist",
        string title = "Scenario Title",
        DateTimeOffset requestTime = default) =>
        GetTracksForAlbumSociableTestEnvironment.ForLookupDataComplete(
            requestTime,
            LookupDataCompleteAlbumTrack.Create(
                LookupDataCompleteAlbumTrackScenarios.DefaultAlbumId,
                artist,
                title,
                "Album Event Album",
                new DateOnly(2025, 2, 3),
                null,
                120000,
                requestTime));
}
