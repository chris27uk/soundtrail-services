using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Events;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForAlbum.Contract;
using Soundtrail.Services.Tests.Unit.Sociable.Features.GetTracksForAlbum.Support;
using Soundtrail.Services.Tests.Unit.Sociable.Features.GetTracksForAlbum;

namespace Soundtrail.Services.Tests.Unit.Sociable.Features.GetTracksForAlbum.Scenarios.LookupDataComplete.Orchestrator;

public sealed class WorkScheduledEventSavedTests
{
    [Fact]
    public async Task Then_The_Target_Is_A_Known_Catalog_Operation()
    {
        var environment = ForCompletedTrack();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForAlbumRequest(environment.AlbumId)));

        Event(environment).Target.Should().BeOfType<EnrichmentTarget.KnownCatalogItemOperation>();
    }

    [Fact]
    public async Task Then_The_Target_Operation_Is_Album_Tracks()
    {
        var environment = ForCompletedTrack();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForAlbumRequest(environment.AlbumId)));

        Operation(environment).Should().BeOfType<CatalogItemOperation.ChildTracksForAlbum>();
    }

    [Fact]
    public async Task Then_The_Target_Contains_The_Album_Id()
    {
        var environment = ForCompletedTrack();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForAlbumRequest(environment.AlbumId)));

        ((CatalogItemOperation.ChildTracksForAlbum)Operation(environment)).Id.Should().Be(environment.AlbumId);
    }

    [Fact]
    public async Task Then_The_Target_Normalised_Identifier_Contains_The_Album_Id()
    {
        var environment = ForCompletedTrack();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForAlbumRequest(environment.AlbumId)));

        Event(environment).Target.NormalisedIdentifier
            .Should().Be($"child_tracks_for_album:{environment.AlbumId.StableValue}");
    }

    [Fact]
    public async Task Then_The_Event_Has_High_Priority()
    {
        var environment = ForCompletedTrack();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForAlbumRequest(environment.AlbumId)));

        Event(environment).Priority.Should().Be(LookupPriorityBand.High);
    }

    [Fact]
    public async Task Then_The_Next_Eligible_Time_Comes_From_The_Request()
    {
        var requestTime = new DateTimeOffset(2026, 8, 2, 11, 12, 0, TimeSpan.Zero);
        var environment = ForCompletedTrack(requestTime);

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForAlbumRequest(environment.AlbumId)));

        Event(environment).NextEligibleAt.Should().Be(requestTime);
    }

    [Fact]
    public async Task Then_The_Earliest_Completion_Is_Twenty_Seconds_After_The_Request()
    {
        var requestTime = new DateTimeOffset(2026, 8, 2, 11, 13, 0, TimeSpan.Zero);
        var environment = ForCompletedTrack(requestTime);

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForAlbumRequest(environment.AlbumId)));

        Event(environment).EarliestExpectedCompletionAt.Should().Be(requestTime.AddSeconds(20));
    }

    [Fact]
    public async Task Then_The_Planner_Reason_Is_Saved()
    {
        var environment = ForCompletedTrack();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForAlbumRequest(environment.AlbumId)));

        Event(environment).Reason.Should().Be("Work is valuable and within coarse planner capacity.");
    }

    [Fact]
    public async Task Then_The_Scheduled_Time_Comes_From_The_Request()
    {
        var requestTime = new DateTimeOffset(2026, 8, 2, 11, 14, 0, TimeSpan.Zero);
        var environment = ForCompletedTrack(requestTime);

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForAlbumRequest(environment.AlbumId)));

        Event(environment).ScheduledAt.Should().Be(requestTime);
    }

    private static GetTracksForAlbumSociableTestEnvironment ForCompletedTrack(DateTimeOffset requestTime = default) =>
        GetTracksForAlbumSociableTestEnvironment.ForLookupDataComplete(
            requestTime,
            LookupDataCompleteAlbumTrackScenarios.MidnightSignals(requestTime));

    private static WorkScheduled Event(GetTracksForAlbumSociableTestEnvironment environment) =>
        environment.SavedEvents<WorkScheduled>()
            .Single(@event => @event.Target.NormalisedIdentifier == $"child_tracks_for_album:{environment.AlbumId.StableValue}");

    private static CatalogItemOperation Operation(GetTracksForAlbumSociableTestEnvironment environment) =>
        ((EnrichmentTarget.KnownCatalogItemOperation)Event(environment).Target).Operation;
}
