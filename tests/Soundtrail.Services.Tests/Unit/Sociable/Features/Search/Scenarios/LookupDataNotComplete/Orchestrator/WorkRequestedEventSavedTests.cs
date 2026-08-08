using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Events;

namespace Soundtrail.Services.Tests.Unit.Sociable.Features.Search.Scenarios.LookupDataNotComplete.Orchestrator;

public sealed class WorkRequestedEventSavedTests
{
    [Fact]
    public async Task Given_The_Request_Is_Orchestrated_Then_The_Event_Target_Is_A_Search()
    {
        var environment = SearchSociableTestEnvironment.ForLookupDataNotComplete();

        await environment.ProjectOnChange(sut => sut.Handle(environment.CreateRequest()));

        environment.SavedEvent<WorkRequested>().Target.Should().BeOfType<EnrichmentTarget.SearchForUnknownCatalogItem>();
    }

    [Fact]
    public async Task Given_The_Request_Is_Orchestrated_Then_The_Target_Contains_The_Search_Criteria()
    {
        var environment = SearchSociableTestEnvironment.ForLookupDataNotComplete();

        await environment.ProjectOnChange(sut => sut.Handle(environment.CreateRequest()));
        var target = (EnrichmentTarget.SearchForUnknownCatalogItem)environment.SavedEvent<WorkRequested>().Target;

        target.Criteria.Should().Be(environment.SearchCriteria);
    }

    [Fact]
    public async Task Given_The_Request_Is_Orchestrated_Then_The_Target_Has_The_Normalised_Search_Identifier()
    {
        var environment = SearchSociableTestEnvironment.ForLookupDataNotComplete();

        await environment.ProjectOnChange(sut => sut.Handle(environment.CreateRequest()));

        environment.SavedEvent<WorkRequested>().Target.NormalisedIdentifier
            .Should().Be(environment.SearchCriteria.NormalisedIdentifier);
    }

    [Fact]
    public async Task Given_The_Request_Is_Orchestrated_Then_The_Event_Has_High_Priority()
    {
        var environment = SearchSociableTestEnvironment.ForLookupDataNotComplete();

        await environment.ProjectOnChange(sut => sut.Handle(environment.CreateRequest()));

        environment.SavedEvent<WorkRequested>().Priority.Should().Be(LookupPriorityBand.High);
    }

    [Fact]
    public async Task Given_The_Request_Is_Orchestrated_Then_The_Event_Has_Full_Trust()
    {
        var environment = SearchSociableTestEnvironment.ForLookupDataNotComplete();

        await environment.ProjectOnChange(sut => sut.Handle(environment.CreateRequest()));

        environment.SavedEvent<WorkRequested>().TrustLevel.Should().Be(100);
    }

    [Fact]
    public async Task Given_The_Request_Is_Orchestrated_Then_The_Event_Has_No_Risk()
    {
        var environment = SearchSociableTestEnvironment.ForLookupDataNotComplete();

        await environment.ProjectOnChange(sut => sut.Handle(environment.CreateRequest()));

        environment.SavedEvent<WorkRequested>().RiskScore.Should().Be(0);
    }

    [Fact]
    public async Task Given_The_Request_Is_Orchestrated_Then_The_Event_Request_Time_Is_Saved()
    {
        var requestTime = new DateTimeOffset(2026, 8, 2, 9, 45, 0, TimeSpan.Zero);
        var environment = SearchSociableTestEnvironment.ForLookupDataNotComplete(requestTime);

        await environment.ProjectOnChange(sut => sut.Handle(environment.CreateRequest()));

        environment.SavedEvent<WorkRequested>().RequestedAt.Should().Be(requestTime);
    }

    [Fact]
    public async Task Given_The_Request_Is_Orchestrated_Then_The_Correlation_Id_Is_Preserved()
    {
        var environment = SearchSociableTestEnvironment.ForLookupDataNotComplete();

        await environment.ProjectOnChange(sut => sut.Handle(environment.CreateRequest()));

        environment.SavedEvent<WorkRequested>().CorrelationId
            .Should().Be(environment.SentMessage<RequestUnknownMusicDataMessage>().CorrelationId);
    }
}
