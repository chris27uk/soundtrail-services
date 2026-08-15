using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Events;
using Soundtrail.Domain.Search;

namespace Soundtrail.Services.Tests.Unit.Solitary.CrossCutting.Orchestrator.OnUnknownMusicDataRequested;

public sealed class UnknownMusicDataRequestsWorkTests
{
    [Fact]
    public async Task Given_The_Search_Target_Has_Already_Been_Requested_With_The_Same_Priority_When_Handling_Then_No_New_Event_Is_Appended()
    {
        var environment = OnUnknownMusicDataRequestedHandlerUnitTestEnvironment.Create();
        var target = Work.SearchExternally(new SearchCriteria("u2", SearchType.Artist));
        environment.Repository.SeedEvents =
        [
            new WorkRequested(target, LookupPriorityBand.High, 50, 5, new DateTimeOffset(2026, 7, 16, 9, 0, 0, TimeSpan.Zero), CorrelationId.From("corr-old"))
        ];
        var subject = environment.CreateSubject();

        await subject.Handle(OnUnknownMusicDataRequestedHandlerUnitTestEnvironment.CreateUnknownRequest(query: "u2", searchType: SearchType.Artist), TestContext.Current.CancellationToken);

        environment.Repository.AppendedEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task Given_The_Search_Target_Has_Already_Been_Requested_At_A_Lower_Priority_When_Handling_Then_A_WorkPriorityRaised_Event_Is_Appended()
    {
        var environment = OnUnknownMusicDataRequestedHandlerUnitTestEnvironment.Create();
        var target = Work.SearchExternally(new SearchCriteria("u2", SearchType.Artist));
        environment.Repository.SeedEvents =
        [
            new WorkRequested(target, LookupPriorityBand.Low, 50, 5, new DateTimeOffset(2026, 7, 16, 9, 0, 0, TimeSpan.Zero), CorrelationId.From("corr-old"))
        ];
        var subject = environment.CreateSubject();

        await subject.Handle(OnUnknownMusicDataRequestedHandlerUnitTestEnvironment.CreateUnknownRequest(query: "u2", searchType: SearchType.Artist, priority: LookupPriorityBand.High, trustLevel: 88, riskScore: 7, correlationId: "corr-new"), TestContext.Current.CancellationToken);

        var raised = environment.Repository.AppendedEvents.Should().ContainSingle().Which.Should().BeOfType<WorkPriorityRaised>().Subject;
        raised.Target.Should().Be(target);
        raised.Priority.Should().Be(LookupPriorityBand.High);
        raised.TrustLevel.Should().Be(88);
        raised.RiskScore.Should().Be(7);
        raised.CorrelationId.Value.Should().Be("corr-new");
    }
}
