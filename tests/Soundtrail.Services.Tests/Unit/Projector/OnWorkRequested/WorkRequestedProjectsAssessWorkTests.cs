using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery.Messages;
using Soundtrail.Domain.Search;

namespace Soundtrail.Services.Tests.Unit.Projector.OnWorkRequested;

public sealed class WorkRequestedProjectsAssessWorkTests
{
    [Fact]
    public async Task Given_A_WorkRequested_Event_When_Projecting_Then_An_AssessWork_Command_Is_Sent()
    {
        var environment = WorkRequestedProjectorUnitTestEnvironment.Create();
        var subject = environment.CreateSubject();

        await subject.Handle(WorkRequestedProjectorUnitTestEnvironment.CreateSearchCriteriaWorkRequested());

        environment.CommandBus.Commands.Should().ContainSingle().Which.Should().BeOfType<AssessWorkMessage>();
    }

    [Fact]
    public async Task Given_A_WorkRequested_Event_When_Projecting_Then_The_Command_Id_Is_Deterministic()
    {
        var environment = WorkRequestedProjectorUnitTestEnvironment.Create();
        var subject = environment.CreateSubject();

        await subject.Handle(
            WorkRequestedProjectorUnitTestEnvironment.CreateSearchCriteriaWorkRequested(
                query: "u2",
                searchType: SearchType.Artist,
                trustLevel: 100,
                riskScore: 0,
                requestedAt: new DateTimeOffset(2026, 7, 15, 8, 11, 0, TimeSpan.Zero),
                correlationId: "correlation-1"));

        environment.CommandBus.Commands.Cast<AssessWorkMessage>().Single().Id.Value
            .Should().Be("AssessWork:search:u2:100:0:correlation-1");
    }

    [Fact]
    public async Task Given_A_WorkRequested_Event_When_Projecting_Then_The_Original_Correlation_Id_Is_Preserved()
    {
        var environment = WorkRequestedProjectorUnitTestEnvironment.Create();
        var subject = environment.CreateSubject();

        await subject.Handle(WorkRequestedProjectorUnitTestEnvironment.CreateSearchCriteriaWorkRequested(correlationId: "corr-42"));

        environment.CommandBus.Commands.Cast<AssessWorkMessage>().Single().CorrelationId.Value.Should().Be("corr-42");
    }

    [Fact]
    public async Task Given_A_WorkRequested_Event_When_Projecting_Then_The_Trust_Level_Is_Preserved()
    {
        var environment = WorkRequestedProjectorUnitTestEnvironment.Create();
        var subject = environment.CreateSubject();

        await subject.Handle(WorkRequestedProjectorUnitTestEnvironment.CreateSearchCriteriaWorkRequested(trustLevel: 77));

        environment.CommandBus.Commands.Cast<AssessWorkMessage>().Single().TrustLevel.Should().Be(77);
    }

    [Fact]
    public async Task Given_A_WorkRequested_Event_When_Projecting_Then_The_Risk_Score_Is_Preserved()
    {
        var environment = WorkRequestedProjectorUnitTestEnvironment.Create();
        var subject = environment.CreateSubject();

        await subject.Handle(WorkRequestedProjectorUnitTestEnvironment.CreateSearchCriteriaWorkRequested(riskScore: 12));

        environment.CommandBus.Commands.Cast<AssessWorkMessage>().Single().RiskScore.Should().Be(12);
    }

    [Fact]
    public async Task Given_A_WorkRequested_Event_When_Projecting_Then_The_Priority_Is_Preserved()
    {
        var environment = WorkRequestedProjectorUnitTestEnvironment.Create();
        var subject = environment.CreateSubject();

        await subject.Handle(
            WorkRequestedProjectorUnitTestEnvironment.CreateSearchCriteriaWorkRequested(priority: LookupPriorityBand.Low));

        environment.CommandBus.Commands.Cast<AssessWorkMessage>().Single().Priority.Should().Be(LookupPriorityBand.Low);
    }
}
