using Soundtrail.Domain.Catalog.Events;
using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForArtist.Contract;
using Soundtrail.Services.Tests.Unit.Sociable.Features.GetTracksForArtist.Support;
using Soundtrail.Services.Tests.Unit.Sociable.Features.GetTracksForArtist;

namespace Soundtrail.Services.Tests.Unit.Sociable.Features.GetTracksForArtist.Scenarios.LookupDataComplete.Orchestrator;

public sealed class TrackDiscoveredEventSavedTests
{
    [Fact]
    public async Task Then_The_Input_Track_Is_Saved()
    {
        const string artist = "Artist Event Artist";
        const string title = "Artist Event Title";
        var expectedTrackId = TrackId.TryCreate(artist, title, "Artist Event Album", new DateOnly(2025, 2, 3), null) switch
        {
            TrackIdCreateResult.Success success => success.Value.Value,
            _ => throw new InvalidOperationException("The test input must produce a track id.")
        };
        var environment = ForCompletedArtistTrack(artist, title);

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForArtistRequest(environment.ArtistId)));

        environment.SavedEvent<TrackDiscovered>().Track.TrackId.Value.Should().Be(expectedTrackId);
    }

    [Fact]
    public async Task Then_The_Track_Title_Is_Saved()
    {
        const string title = "Artist Event Title";
        var environment = ForCompletedArtistTrack(title: title);

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForArtistRequest(environment.ArtistId)));

        environment.SavedEvent<TrackDiscovered>().Track.Title.Should().Be(title);
    }

    [Fact]
    public async Task Then_The_Observed_Time_Comes_From_The_Request()
    {
        var requestTime = new DateTimeOffset(2026, 8, 2, 11, 1, 0, TimeSpan.Zero);
        var environment = ForCompletedArtistTrack(requestTime: requestTime);

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForArtistRequest(environment.ArtistId)));

        environment.SavedEvent<TrackDiscovered>().ObservedAt.Should().Be(requestTime);
    }

    private static GetTracksForArtistSociableTestEnvironment ForCompletedArtistTrack(
        string artist = "Scenario Artist",
        string title = "Scenario Title",
        DateTimeOffset requestTime = default) =>
        GetTracksForArtistSociableTestEnvironment.ForLookupDataComplete(
            requestTime,
            LookupDataCompleteArtistTrack.Create(
                LookupDataCompleteArtistTrackScenarios.DefaultArtistId,
                artist,
                title,
                "Artist Event Album",
                new DateOnly(2025, 2, 3),
                null,
                120000,
                requestTime));
}
