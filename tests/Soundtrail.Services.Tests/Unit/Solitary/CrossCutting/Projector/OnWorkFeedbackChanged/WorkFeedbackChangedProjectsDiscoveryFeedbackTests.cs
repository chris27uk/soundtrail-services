using Soundtrail.Adapters.Projection;
using Soundtrail.Domain.Discovery.Events;
using Soundtrail.Services.Internal.Projector.Features.OnWorkFeedbackChanged.OnWorkCompleted;

namespace Soundtrail.Services.Tests.Unit.Solitary.CrossCutting.Projector.OnWorkFeedbackChanged;

public sealed class WorkFeedbackChangedProjectsDiscoveryFeedbackTests
{
    [Fact]
    public void Given_A_Work_Requested_Event_When_Projecting_Then_Public_Feedback_Is_Not_Updated()
    {
        typeof(WorkCompletedEventHandler).Assembly
            .GetTypes()
            .Where(static type => type.Namespace?.Contains("OnWorkFeedbackChanged", StringComparison.Ordinal) == true)
            .Where(static type => type
                .GetInterfaces()
                .Any(static contract =>
                    contract.IsGenericType
                    && contract.GetGenericTypeDefinition() == typeof(IProjectionEventHandler<>)
                    && contract.GenericTypeArguments[0] == typeof(WorkRequested)))
            .Should()
            .BeEmpty();
    }

    [Fact]
    public async Task Given_A_Work_Deferred_Event_When_Projecting_Then_Feedback_Is_Updated()
    {
        var environment = WorkFeedbackChangedProjectorUnitTestEnvironment.Create();
        var subject = environment.CreateDeferredHandler();
        var @event = WorkFeedbackChangedProjectorUnitTestEnvironment.CreateDeferred();

        await subject.HandleAsync(@event, TestContext.Current.CancellationToken);

        environment.StoreDiscoveryFeedbackPort.StoredEvent.Should().BeSameAs(@event);
    }

    [Fact]
    public async Task Given_A_Work_Rejected_Event_When_Projecting_Then_Feedback_Is_Updated()
    {
        var environment = WorkFeedbackChangedProjectorUnitTestEnvironment.Create();
        var subject = environment.CreateRejectedHandler();
        var @event = WorkFeedbackChangedProjectorUnitTestEnvironment.CreateRejected();

        await subject.HandleAsync(@event, TestContext.Current.CancellationToken);

        environment.StoreDiscoveryFeedbackPort.StoredEvent.Should().BeSameAs(@event);
    }

    [Fact]
    public async Task Given_A_Work_Ignored_Event_When_Projecting_Then_Feedback_Is_Updated()
    {
        var environment = WorkFeedbackChangedProjectorUnitTestEnvironment.Create();
        var subject = environment.CreateIgnoredHandler();
        var @event = WorkFeedbackChangedProjectorUnitTestEnvironment.CreateIgnored();

        await subject.HandleAsync(@event, TestContext.Current.CancellationToken);

        environment.StoreDiscoveryFeedbackPort.StoredEvent.Should().BeSameAs(@event);
    }

    [Fact]
    public async Task Given_A_Work_Attempt_Failed_Event_When_Projecting_Then_Feedback_Is_Updated()
    {
        var environment = WorkFeedbackChangedProjectorUnitTestEnvironment.Create();
        var subject = environment.CreateAttemptFailedHandler();
        var @event = WorkFeedbackChangedProjectorUnitTestEnvironment.CreateAttemptFailed();

        await subject.HandleAsync(@event, TestContext.Current.CancellationToken);

        environment.StoreDiscoveryFeedbackPort.StoredEvent.Should().BeSameAs(@event);
    }
}
