using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Events;
using Soundtrail.Services.Api.Features.Catalog.Search.Contract;

namespace Soundtrail.Services.Tests.Unit.Sociable.Search.LookupDataComplete.Orchestrator;

public sealed class WorkCompletedEventSavedTests
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
    public async Task Then_The_Completion_Reason_Is_Saved()
    {
        var environment = ForCompletedArtist();

        await environment.ProjectOnChange(sut => sut.Handle(environment.CreateRequest()));

        Event(environment).Reason.Should().Be("Lookup completed.");
    }

    [Fact]
    public async Task Then_The_Completed_Time_Comes_From_The_Request()
    {
        var requestTime = new DateTimeOffset(2026, 8, 2, 12, 11, 0, TimeSpan.Zero);
        var environment = ForCompletedArtist(requestTime);

        await environment.ProjectOnChange(sut => sut.Handle(environment.CreateRequest()));

        Event(environment).CompletedAt.Should().Be(requestTime);
    }

    private static SearchSociableTestEnvironment ForCompletedArtist(DateTimeOffset requestTime = default) =>
        SearchSociableTestEnvironment.ForLookupDataComplete(
            requestTime,
            LookupDataCompleteSearchScenarios.AuroraLane());

    private static WorkCompleted Event(SearchSociableTestEnvironment environment) =>
        environment.SavedEvents<WorkCompleted>()
            .First(@event => @event.Target.NormalisedIdentifier == environment.SearchCriteria.NormalisedIdentifier);
}
