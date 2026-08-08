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

        environment.CommandBus.SentMessages.Should().ContainSingle().Which.Should().BeOfType<AssessWorkMessage>();
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

        var firstId = environment.CommandBus.SentMessages.Cast<AssessWorkMessage>().Single().Id.Value;

        await subject.Handle(
            WorkRequestedProjectorUnitTestEnvironment.CreateSearchCriteriaWorkRequested(
                query: "u2",
                searchType: SearchType.Artist,
                trustLevel: 100,
                riskScore: 0,
                requestedAt: new DateTimeOffset(2026, 7, 15, 8, 11, 0, TimeSpan.Zero),
                correlationId: "correlation-1"));

        var secondId = environment.CommandBus.SentMessages.Cast<AssessWorkMessage>().Last().Id.Value;

        firstId.Should().Be(secondId);
        firstId.Should().StartWith("AssessWork:");
    }

    [Fact]
    public async Task Given_A_Long_Search_Target_When_Projecting_Then_The_Command_Id_Fits_Service_Bus_Limits()
    {
        var environment = WorkRequestedProjectorUnitTestEnvironment.Create();
        var subject = environment.CreateSubject();

        await subject.Handle(
            WorkRequestedProjectorUnitTestEnvironment.CreateSearchCriteriaWorkRequested(
                query: "Midnight Signals Aurora Lane (2024 Remake Radio Edit Extended Version Featuring Someone Else)",
                searchType: SearchType.Track,
                correlationId: "child_tracks_for_playlist:worldtop100:search:midnight-signals-aurora-lane-2024-remake-radio-edit-extended-version-featuring-someone-else"));

        environment.CommandBus.SentMessages.Cast<AssessWorkMessage>().Single().Id.Value.Length.Should().BeLessThanOrEqualTo(128);
    }

    [Fact]
    public async Task Given_A_WorkRequested_Event_When_Projecting_Then_The_Original_Correlation_Id_Is_Preserved()
    {
        var environment = WorkRequestedProjectorUnitTestEnvironment.Create();
        var subject = environment.CreateSubject();

        await subject.Handle(WorkRequestedProjectorUnitTestEnvironment.CreateSearchCriteriaWorkRequested(correlationId: "corr-42"));

        environment.CommandBus.SentMessages.Cast<AssessWorkMessage>().Single().CorrelationId.Value.Should().Be("corr-42");
    }

    [Fact]
    public async Task Given_A_WorkRequested_Event_When_Projecting_Then_The_Trust_Level_Is_Preserved()
    {
        var environment = WorkRequestedProjectorUnitTestEnvironment.Create();
        var subject = environment.CreateSubject();

        await subject.Handle(WorkRequestedProjectorUnitTestEnvironment.CreateSearchCriteriaWorkRequested(trustLevel: 77));

        environment.CommandBus.SentMessages.Cast<AssessWorkMessage>().Single().TrustLevel.Should().Be(77);
    }

    [Fact]
    public async Task Given_A_WorkRequested_Event_When_Projecting_Then_The_Risk_Score_Is_Preserved()
    {
        var environment = WorkRequestedProjectorUnitTestEnvironment.Create();
        var subject = environment.CreateSubject();

        await subject.Handle(WorkRequestedProjectorUnitTestEnvironment.CreateSearchCriteriaWorkRequested(riskScore: 12));

        environment.CommandBus.SentMessages.Cast<AssessWorkMessage>().Single().RiskScore.Should().Be(12);
    }

    [Fact]
    public async Task Given_A_WorkRequested_Event_When_Projecting_Then_The_Priority_Is_Preserved()
    {
        var environment = WorkRequestedProjectorUnitTestEnvironment.Create();
        var subject = environment.CreateSubject();

        await subject.Handle(
            WorkRequestedProjectorUnitTestEnvironment.CreateSearchCriteriaWorkRequested(priority: LookupPriorityBand.Low));

        environment.CommandBus.SentMessages.Cast<AssessWorkMessage>().Single().Priority.Should().Be(LookupPriorityBand.Low);
    }
}
