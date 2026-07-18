using Soundtrail.Domain.Discovery.Assesment;
using Soundtrail.Domain.Search;
using Soundtrail.Services.Api.Features.Search.Contract;
using Soundtrail.Services.Projector.Features.OnMusicDataRequested;

namespace Soundtrail.Services.Tests.Unit.Projector.OnMusicDataRequested;

public sealed class WorkRequestedProjectsAssessWorkTests
{
    [Fact]
    public async Task Given_A_WorkRequested_Event_When_Projecting_Then_An_AssessWork_Command_Is_Sent()
    {
        var environment = WorkRequestedProjectorUnitTestEnvironment.Create();
        var subject = environment.CreateSubject();

        await subject.Handle(WorkRequestedProjectorUnitTestEnvironment.CreateSearchCriteriaWorkRequested());

        environment.CommandBus.Commands.Should().ContainSingle().Which.Should().BeOfType<AssessWorkCommand>();
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

        environment.CommandBus.Commands.Cast<AssessWorkCommand>().Single().CommandId.Value
            .Should().Be("AssessWork:search:u2:100:0:correlation-1");
    }

    [Fact]
    public async Task Given_A_WorkRequested_Event_When_Projecting_Then_The_Original_Correlation_Id_Is_Preserved()
    {
        var environment = WorkRequestedProjectorUnitTestEnvironment.Create();
        var subject = environment.CreateSubject();

        await subject.Handle(WorkRequestedProjectorUnitTestEnvironment.CreateSearchCriteriaWorkRequested(correlationId: "corr-42"));

        environment.CommandBus.Commands.Cast<AssessWorkCommand>().Single().CorrelationId.Value.Should().Be("corr-42");
    }

    [Fact]
    public async Task Given_A_WorkRequested_Event_When_Projecting_Then_The_Trust_Level_Is_Preserved()
    {
        var environment = WorkRequestedProjectorUnitTestEnvironment.Create();
        var subject = environment.CreateSubject();

        await subject.Handle(WorkRequestedProjectorUnitTestEnvironment.CreateSearchCriteriaWorkRequested(trustLevel: 77));

        environment.CommandBus.Commands.Cast<AssessWorkCommand>().Single().TrustLevel.Should().Be(77);
    }

    [Fact]
    public async Task Given_A_WorkRequested_Event_When_Projecting_Then_The_Risk_Score_Is_Preserved()
    {
        var environment = WorkRequestedProjectorUnitTestEnvironment.Create();
        var subject = environment.CreateSubject();

        await subject.Handle(WorkRequestedProjectorUnitTestEnvironment.CreateSearchCriteriaWorkRequested(riskScore: 12));

        environment.CommandBus.Commands.Cast<AssessWorkCommand>().Single().RiskScore.Should().Be(12);
    }
}
