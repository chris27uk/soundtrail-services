using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Events;
using Soundtrail.Services.Api.Features.Catalog.Search.Contract;

namespace Soundtrail.Services.Tests.Unit.Sociable.Search.LookupDataComplete.Orchestrator;

public sealed class WorkScheduledEventSavedTests
{
    [Fact]
    public async Task Then_The_Target_Is_A_Search()
    {
        var environment = ForCompletedArtist();

        await environment.ProjectOnChange(sut => sut.Handle(environment.CreateRequest()));

        Event(environment).Target.Should().BeOfType<EnrichmentTarget.SearchForUnknownCatalogItem>();
    }

    [Fact]
    public async Task Then_The_Target_Contains_The_Search_Criteria()
    {
        var environment = ForCompletedArtist();

        await environment.ProjectOnChange(sut => sut.Handle(environment.CreateRequest()));
        var target = (EnrichmentTarget.SearchForUnknownCatalogItem)Event(environment).Target;

        target.Criteria.Should().Be(environment.SearchCriteria);
    }

    [Fact]
    public async Task Then_The_Target_Has_The_Normalised_Search_Identifier()
    {
        var environment = ForCompletedArtist();

        await environment.ProjectOnChange(sut => sut.Handle(environment.CreateRequest()));

        Event(environment).Target.NormalisedIdentifier
            .Should().Be(environment.SearchCriteria.NormalisedIdentifier);
    }

    [Fact]
    public async Task Then_The_Event_Has_High_Priority()
    {
        var environment = ForCompletedArtist();

        await environment.ProjectOnChange(sut => sut.Handle(environment.CreateRequest()));

        Event(environment).Priority.Should().Be(LookupPriorityBand.High);
    }

    [Fact]
    public async Task Then_The_Next_Eligible_Time_Comes_From_The_Request()
    {
        var requestTime = new DateTimeOffset(2026, 8, 2, 11, 12, 0, TimeSpan.Zero);
        var environment = ForCompletedArtist(requestTime);

        await environment.ProjectOnChange(sut => sut.Handle(environment.CreateRequest()));

        Event(environment).NextEligibleAt.Should().Be(requestTime);
    }

    [Fact]
    public async Task Then_The_Earliest_Completion_Is_Forty_Five_Seconds_After_The_Request()
    {
        var requestTime = new DateTimeOffset(2026, 8, 2, 11, 13, 0, TimeSpan.Zero);
        var environment = ForCompletedArtist(requestTime);

        await environment.ProjectOnChange(sut => sut.Handle(environment.CreateRequest()));

        Event(environment).EarliestExpectedCompletionAt.Should().Be(requestTime.AddSeconds(45));
    }

    [Fact]
    public async Task Then_The_Planner_Reason_Is_Saved()
    {
        var environment = ForCompletedArtist();

        await environment.ProjectOnChange(sut => sut.Handle(environment.CreateRequest()));

        Event(environment).Reason.Should().Be("Work is valuable and within coarse planner capacity.");
    }

    [Fact]
    public async Task Then_The_Scheduled_Time_Comes_From_The_Request()
    {
        var requestTime = new DateTimeOffset(2026, 8, 2, 11, 14, 0, TimeSpan.Zero);
        var environment = ForCompletedArtist(requestTime);

        await environment.ProjectOnChange(sut => sut.Handle(environment.CreateRequest()));

        Event(environment).ScheduledAt.Should().Be(requestTime);
    }

    private static SearchSociableTestEnvironment ForCompletedArtist(DateTimeOffset requestTime = default) =>
        SearchSociableTestEnvironment.ForLookupDataComplete(
            requestTime,
            LookupDataCompleteSearchScenarios.AuroraLane());

    private static WorkScheduled Event(SearchSociableTestEnvironment environment) =>
        environment.SavedEvents<WorkScheduled>()
            .Single(@event => @event.Target.NormalisedIdentifier == environment.SearchCriteria.NormalisedIdentifier);
}
