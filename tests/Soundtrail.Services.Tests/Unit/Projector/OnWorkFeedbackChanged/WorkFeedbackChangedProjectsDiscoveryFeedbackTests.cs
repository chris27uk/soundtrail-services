using Soundtrail.Domain.Discovery.Events;

namespace Soundtrail.Services.Tests.Unit.Projector.OnWorkFeedbackChanged;

public sealed class WorkFeedbackChangedProjectsDiscoveryFeedbackTests
{
    [Fact]
    public async Task Given_A_Work_Requested_Event_When_Projecting_Then_Feedback_Is_Updated()
    {
        var environment = WorkFeedbackChangedProjectorUnitTestEnvironment.Create();
        var subject = environment.CreateSubject();
        var @event = WorkFeedbackChangedProjectorUnitTestEnvironment.CreateRequested();

        await subject.Handle(@event);

        environment.StoreDiscoveryFeedbackPort.StoredEvent.Should().BeSameAs(@event);
    }

    [Fact]
    public async Task Given_A_Work_Scheduled_Event_When_Projecting_Then_Feedback_Is_Updated()
    {
        var environment = WorkFeedbackChangedProjectorUnitTestEnvironment.Create();
        var subject = environment.CreateSubject();
        var @event = WorkFeedbackChangedProjectorUnitTestEnvironment.CreateScheduled();

        await subject.Handle(@event);

        environment.StoreDiscoveryFeedbackPort.StoredEvent.Should().BeSameAs(@event);
    }

    [Fact]
    public async Task Given_A_Work_Deferred_Event_When_Projecting_Then_Feedback_Is_Updated()
    {
        var environment = WorkFeedbackChangedProjectorUnitTestEnvironment.Create();
        var subject = environment.CreateSubject();
        var @event = WorkFeedbackChangedProjectorUnitTestEnvironment.CreateDeferred();

        await subject.Handle(@event);

        environment.StoreDiscoveryFeedbackPort.StoredEvent.Should().BeSameAs(@event);
    }

    [Fact]
    public async Task Given_A_Work_Completed_Event_When_Projecting_Then_Feedback_Is_Updated()
    {
        var environment = WorkFeedbackChangedProjectorUnitTestEnvironment.Create();
        var subject = environment.CreateSubject();
        var @event = WorkFeedbackChangedProjectorUnitTestEnvironment.CreateCompleted();

        await subject.Handle(@event);

        environment.StoreDiscoveryFeedbackPort.StoredEvent.Should().BeSameAs(@event);
    }

    [Fact]
    public async Task Given_A_Work_Rejected_Event_When_Projecting_Then_Feedback_Is_Updated()
    {
        var environment = WorkFeedbackChangedProjectorUnitTestEnvironment.Create();
        var subject = environment.CreateSubject();
        var @event = WorkFeedbackChangedProjectorUnitTestEnvironment.CreateRejected();

        await subject.Handle(@event);

        environment.StoreDiscoveryFeedbackPort.StoredEvent.Should().BeSameAs(@event);
    }

    [Fact]
    public async Task Given_A_Work_Ignored_Event_When_Projecting_Then_Feedback_Is_Updated()
    {
        var environment = WorkFeedbackChangedProjectorUnitTestEnvironment.Create();
        var subject = environment.CreateSubject();
        var @event = WorkFeedbackChangedProjectorUnitTestEnvironment.CreateIgnored();

        await subject.Handle(@event);

        environment.StoreDiscoveryFeedbackPort.StoredEvent.Should().BeSameAs(@event);
    }

    [Fact]
    public async Task Given_A_Work_Attempt_Failed_Event_When_Projecting_Then_Feedback_Is_Updated()
    {
        var environment = WorkFeedbackChangedProjectorUnitTestEnvironment.Create();
        var subject = environment.CreateSubject();
        var @event = WorkFeedbackChangedProjectorUnitTestEnvironment.CreateAttemptFailed();

        await subject.Handle(@event);

        environment.StoreDiscoveryFeedbackPort.StoredEvent.Should().BeSameAs(@event);
    }
}
