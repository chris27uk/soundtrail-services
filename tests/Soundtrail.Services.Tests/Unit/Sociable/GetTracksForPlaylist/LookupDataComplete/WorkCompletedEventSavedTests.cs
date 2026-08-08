using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Events;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForPlaylist.Contract;

namespace Soundtrail.Services.Tests.Unit.Sociable.GetTracksForPlaylist.LookupDataComplete;

public sealed class WorkCompletedEventSavedTests
{
    [Fact]
    public async Task Then_The_Target_Is_A_Known_Catalog_Operation()
    {
        var environment = ForCompletedTrack();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForPlaylistRequest(environment.PlaylistId)));

        Event(environment).Target.Should().BeOfType<EnrichmentTarget.KnownCatalogItemOperation>();
    }

    [Fact]
    public async Task Then_The_Target_Operation_Is_Playlist_Tracks()
    {
        var environment = ForCompletedTrack();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForPlaylistRequest(environment.PlaylistId)));

        Operation(environment).Should().BeOfType<CatalogItemOperation.ChildTracksForPlaylist>();
    }

    [Fact]
    public async Task Then_The_Target_Contains_The_Playlist_Id()
    {
        var environment = ForCompletedTrack();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForPlaylistRequest(environment.PlaylistId)));

        ((CatalogItemOperation.ChildTracksForPlaylist)Operation(environment)).Id.Should().Be(environment.PlaylistId);
    }

    [Fact]
    public async Task Then_The_Target_Normalised_Identifier_Contains_The_Playlist_Id()
    {
        var environment = ForCompletedTrack();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForPlaylistRequest(environment.PlaylistId)));

        Event(environment).Target.NormalisedIdentifier.Should().Be($"child_tracks_for_playlist:{environment.PlaylistId.Value}");
    }

    [Fact]
    public async Task Then_The_Event_Has_High_Priority()
    {
        var environment = ForCompletedTrack();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForPlaylistRequest(environment.PlaylistId)));

        Event(environment).Priority.Should().Be(LookupPriorityBand.High);
    }

    [Fact]
    public async Task Then_The_Completion_Reason_Is_Saved()
    {
        var environment = ForCompletedTrack();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForPlaylistRequest(environment.PlaylistId)));

        Event(environment).Reason.Should().Be("Lookup completed.");
    }

    [Fact]
    public async Task Then_The_Completed_Time_Comes_From_The_Request()
    {
        var requestTime = new DateTimeOffset(2026, 8, 2, 12, 11, 0, TimeSpan.Zero);
        var environment = ForCompletedTrack(requestTime);

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForPlaylistRequest(environment.PlaylistId)));

        Event(environment).CompletedAt.Should().Be(requestTime);
    }

    private static GetTracksForPlaylistSociableTestEnvironment ForCompletedTrack(DateTimeOffset requestTime = default) =>
        GetTracksForPlaylistSociableTestEnvironment.ForLookupDataComplete(
            requestTime,
            LookupDataCompleteTrackScenarios.MidnightSignals(requestTime));

    private static WorkCompleted Event(GetTracksForPlaylistSociableTestEnvironment environment) =>
        environment.SavedEvents<WorkCompleted>()
            .First(@event => @event.Target.NormalisedIdentifier == $"child_tracks_for_playlist:{environment.PlaylistId.Value}");

    private static CatalogItemOperation Operation(GetTracksForPlaylistSociableTestEnvironment environment) =>
        ((EnrichmentTarget.KnownCatalogItemOperation)Event(environment).Target).Operation;
}
