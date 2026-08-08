using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Events;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForArtist.Contract;

namespace Soundtrail.Services.Tests.Unit.Sociable.GetTracksForArtist.LookupDataComplete.Orchestrator;

public sealed class WorkCompletedEventSavedTests
{
    [Fact]
    public async Task Then_The_Target_Is_A_Known_Catalog_Operation()
    {
        var environment = ForCompletedTrack();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForArtistRequest(environment.ArtistId)));

        Event(environment).Target.Should().BeOfType<EnrichmentTarget.KnownCatalogItemOperation>();
    }

    [Fact]
    public async Task Then_The_Target_Operation_Is_Artist_Tracks()
    {
        var environment = ForCompletedTrack();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForArtistRequest(environment.ArtistId)));

        Operation(environment).Should().BeOfType<CatalogItemOperation.ChildTracksForArtist>();
    }

    [Fact]
    public async Task Then_The_Target_Contains_The_Artist_Id()
    {
        var environment = ForCompletedTrack();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForArtistRequest(environment.ArtistId)));

        ((CatalogItemOperation.ChildTracksForArtist)Operation(environment)).Id.Should().Be(environment.ArtistId);
    }

    [Fact]
    public async Task Then_The_Target_Normalised_Identifier_Contains_The_Artist_Id()
    {
        var environment = ForCompletedTrack();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForArtistRequest(environment.ArtistId)));

        Event(environment).Target.NormalisedIdentifier
            .Should().Be($"child_tracks_for_artist:{environment.ArtistId.Value}");
    }

    [Fact]
    public async Task Then_The_Event_Has_High_Priority()
    {
        var environment = ForCompletedTrack();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForArtistRequest(environment.ArtistId)));

        Event(environment).Priority.Should().Be(LookupPriorityBand.High);
    }

    [Fact]
    public async Task Then_The_Completion_Reason_Is_Saved()
    {
        var environment = ForCompletedTrack();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForArtistRequest(environment.ArtistId)));

        Event(environment).Reason.Should().Be("Lookup completed.");
    }

    [Fact]
    public async Task Then_The_Completed_Time_Comes_From_The_Request()
    {
        var requestTime = new DateTimeOffset(2026, 8, 2, 12, 11, 0, TimeSpan.Zero);
        var environment = ForCompletedTrack(requestTime);

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForArtistRequest(environment.ArtistId)));

        Event(environment).CompletedAt.Should().Be(requestTime);
    }

    private static GetTracksForArtistSociableTestEnvironment ForCompletedTrack(DateTimeOffset requestTime = default) =>
        GetTracksForArtistSociableTestEnvironment.ForLookupDataComplete(
            requestTime,
            LookupDataCompleteArtistTrackScenarios.MidnightSignals(requestTime));

    private static WorkCompleted Event(GetTracksForArtistSociableTestEnvironment environment) =>
        environment.SavedEvents<WorkCompleted>()
            .First(@event => @event.Target.NormalisedIdentifier == $"child_tracks_for_artist:{environment.ArtistId.Value}");

    private static CatalogItemOperation Operation(GetTracksForArtistSociableTestEnvironment environment) =>
        ((EnrichmentTarget.KnownCatalogItemOperation)Event(environment).Target).Operation;
}
