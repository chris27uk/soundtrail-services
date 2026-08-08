using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Events;
using Soundtrail.Services.Api.Features.Catalog.GetAlbumsForArtist.Contract;

namespace Soundtrail.Services.Tests.Unit.Sociable.GetAlbumsForArtist.LookupDataComplete.Orchestrator;

public sealed class WorkScheduledEventSavedTests
{
    [Fact]
    public async Task Then_The_Target_Is_A_Known_Catalog_Operation()
    {
        var environment = ForCompletedAlbum();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetAlbumsForArtistRequest(environment.ArtistId)));

        Event(environment).Target.Should().BeOfType<EnrichmentTarget.KnownCatalogItemOperation>();
    }

    [Fact]
    public async Task Then_The_Target_Operation_Is_Artist_Albums()
    {
        var environment = ForCompletedAlbum();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetAlbumsForArtistRequest(environment.ArtistId)));

        Operation(environment).Should().BeOfType<CatalogItemOperation.ChildAlbumsForArtist>();
    }

    [Fact]
    public async Task Then_The_Target_Contains_The_Artist_Id()
    {
        var environment = ForCompletedAlbum();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetAlbumsForArtistRequest(environment.ArtistId)));

        ((CatalogItemOperation.ChildAlbumsForArtist)Operation(environment)).Id.Should().Be(environment.ArtistId);
    }

    [Fact]
    public async Task Then_The_Target_Normalised_Identifier_Contains_The_Artist_Id()
    {
        var environment = ForCompletedAlbum();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetAlbumsForArtistRequest(environment.ArtistId)));

        Event(environment).Target.NormalisedIdentifier
            .Should().Be($"child_albums_for_artist:{environment.ArtistId.Value}");
    }

    [Fact]
    public async Task Then_The_Event_Has_High_Priority()
    {
        var environment = ForCompletedAlbum();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetAlbumsForArtistRequest(environment.ArtistId)));

        Event(environment).Priority.Should().Be(LookupPriorityBand.High);
    }

    [Fact]
    public async Task Then_The_Next_Eligible_Time_Comes_From_The_Request()
    {
        var requestTime = new DateTimeOffset(2026, 8, 2, 11, 12, 0, TimeSpan.Zero);
        var environment = ForCompletedAlbum(requestTime);

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetAlbumsForArtistRequest(environment.ArtistId)));

        Event(environment).NextEligibleAt.Should().Be(requestTime);
    }

    [Fact]
    public async Task Then_The_Earliest_Completion_Is_Twenty_Seconds_After_The_Request()
    {
        var requestTime = new DateTimeOffset(2026, 8, 2, 11, 13, 0, TimeSpan.Zero);
        var environment = ForCompletedAlbum(requestTime);

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetAlbumsForArtistRequest(environment.ArtistId)));

        Event(environment).EarliestExpectedCompletionAt.Should().Be(requestTime.AddSeconds(20));
    }

    [Fact]
    public async Task Then_The_Planner_Reason_Is_Saved()
    {
        var environment = ForCompletedAlbum();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetAlbumsForArtistRequest(environment.ArtistId)));

        Event(environment).Reason.Should().Be("Work is valuable and within coarse planner capacity.");
    }

    [Fact]
    public async Task Then_The_Scheduled_Time_Comes_From_The_Request()
    {
        var requestTime = new DateTimeOffset(2026, 8, 2, 11, 14, 0, TimeSpan.Zero);
        var environment = ForCompletedAlbum(requestTime);

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetAlbumsForArtistRequest(environment.ArtistId)));

        Event(environment).ScheduledAt.Should().Be(requestTime);
    }

    private static GetAlbumsForArtistSociableTestEnvironment ForCompletedAlbum(DateTimeOffset requestTime = default) =>
        GetAlbumsForArtistSociableTestEnvironment.ForLookupDataComplete(
            requestTime,
            LookupDataCompleteArtistAlbumScenarios.MidnightSignals(requestTime));

    private static WorkScheduled Event(GetAlbumsForArtistSociableTestEnvironment environment) =>
        environment.SavedEvents<WorkScheduled>()
            .Single(@event => @event.Target.NormalisedIdentifier == $"child_albums_for_artist:{environment.ArtistId.Value}");

    private static CatalogItemOperation Operation(GetAlbumsForArtistSociableTestEnvironment environment) =>
        ((EnrichmentTarget.KnownCatalogItemOperation)Event(environment).Target).Operation;
}
