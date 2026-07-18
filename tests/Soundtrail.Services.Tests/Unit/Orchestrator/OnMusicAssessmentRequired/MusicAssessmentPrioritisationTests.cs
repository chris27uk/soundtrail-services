using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery.Events;

namespace Soundtrail.Services.Tests.Unit.Orchestrator.OnMusicAssessmentRequired;

public sealed class MusicAssessmentPrioritisationTests
{
    [Fact]
    public async Task Given_A_High_Priority_Request_When_Capacity_Is_Available_Then_Work_Is_Scheduled_With_Planning_Feedback()
    {
        var environment = OnMusicAssessmentRequiredHandlerUnitTestEnvironment.Create();
        var subject = environment.CreateSubject();

        await subject.Handle(OnMusicAssessmentRequiredHandlerUnitTestEnvironment.CreateRequest());

        environment.Repository.AppendedEvents.OfType<WorkScheduled>().Single().EarliestExpectedCompletionAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Given_A_Low_Priority_Request_When_High_Priority_Headroom_Is_Reserved_Then_Work_Is_Deferred()
    {
        var environment = OnMusicAssessmentRequiredHandlerUnitTestEnvironment.Create(new Soundtrail.Services.Enrichment.Orchestrator.Features.OnMusicAssessmentRequired.Planning.PlanningAssessmentOptions
        {
            MaxConcurrentPlannedWork = 10,
            ReservedSlotsForHighPriority = 3,
            DefaultDeferredSeconds = 45
        });
        environment.ProjectionReader.ProjectionToReturn = new(false, null, 7, 3);
        var subject = environment.CreateSubject();

        await subject.Handle(OnMusicAssessmentRequiredHandlerUnitTestEnvironment.CreateRequest(priority: LookupPriorityBand.Low));

        var deferred = environment.Repository.AppendedEvents.OfType<WorkDeferred>().Single();
        deferred.NextEligibleAt.Should().BeAfter(deferred.DeferredAt);
        deferred.EstimatedRetryAfterSeconds.Should().Be(45);
    }

    [Fact]
    public async Task Given_Equivalent_Work_Already_In_Flight_When_Assessing_Then_The_Request_Is_Ignored_With_An_Expected_Completion_Estimate()
    {
        var expectedCompletion = new DateTimeOffset(2026, 7, 18, 9, 31, 0, TimeSpan.Zero);
        var environment = OnMusicAssessmentRequiredHandlerUnitTestEnvironment.Create();
        environment.ProjectionReader.ProjectionToReturn = new(true, expectedCompletion, 1, 1);
        var subject = environment.CreateSubject();

        await subject.Handle(OnMusicAssessmentRequiredHandlerUnitTestEnvironment.CreateRequest());

        var ignored = environment.Repository.AppendedEvents.OfType<WorkIgnored>().Single();
        ignored.Reason.Should().Contain("already planned");
        ignored.EarliestExpectedCompletionAt.Should().Be(expectedCompletion);
    }
}
