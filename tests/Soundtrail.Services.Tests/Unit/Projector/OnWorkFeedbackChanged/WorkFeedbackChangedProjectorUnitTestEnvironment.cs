using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Events;
using Soundtrail.Domain.Common;
using Soundtrail.Services.Internal.Projector.Features.OnWorkFeedbackChanged;
using Soundtrail.Services.Internal.Projector.Features.OnWorkFeedbackChanged.Adapters;

namespace Soundtrail.Services.Tests.Unit.Projector.OnWorkFeedbackChanged;

internal sealed class WorkFeedbackChangedProjectorUnitTestEnvironment
{
    private WorkFeedbackChangedProjectorUnitTestEnvironment(
        StoreDiscoveryFeedbackPortFake storeDiscoveryFeedbackPort)
    {
        StoreDiscoveryFeedbackPort = storeDiscoveryFeedbackPort;
    }

    public StoreDiscoveryFeedbackPortFake StoreDiscoveryFeedbackPort { get; }

    public static WorkFeedbackChangedProjectorUnitTestEnvironment Create() =>
        new(new StoreDiscoveryFeedbackPortFake());

    public WorkFeedbackChangedProjectorHandler CreateSubject() => new(StoreDiscoveryFeedbackPort);

    public static WorkRequested CreateRequested() =>
        new(
            Work.EnrichTrackStreamingLocation(TestTrackIds.Create("feedback-track-requested")),
            LookupPriorityBand.High,
            100,
            0,
            new DateTimeOffset(2026, 7, 19, 11, 0, 0, TimeSpan.Zero),
            CorrelationId.From("feedback-requested"));

    public static WorkScheduled CreateScheduled() =>
        new(
            Work.EnrichTrackStreamingLocation(TestTrackIds.Create("feedback-track-scheduled")),
            LookupPriorityBand.Low,
            new DateTimeOffset(2026, 7, 19, 11, 1, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 7, 19, 11, 3, 0, TimeSpan.Zero),
            "Scheduled",
            new DateTimeOffset(2026, 7, 19, 11, 0, 30, TimeSpan.Zero));

    public static WorkDeferred CreateDeferred() =>
        new(
            Work.EnrichTrackStreamingLocation(TestTrackIds.Create("feedback-track-deferred")),
            LookupPriorityBand.Low,
            new DateTimeOffset(2026, 7, 19, 11, 10, 0, TimeSpan.Zero),
            45,
            "Rate limited",
            new DateTimeOffset(2026, 7, 19, 11, 1, 0, TimeSpan.Zero));

    public static WorkCompleted CreateCompleted() =>
        new(
            Work.EnrichTrackStreamingLocation(TestTrackIds.Create("feedback-track-completed")),
            LookupPriorityBand.High,
            "Done",
            new DateTimeOffset(2026, 7, 19, 11, 2, 0, TimeSpan.Zero));

    public static WorkRejected CreateRejected() =>
        new(
            Work.EnrichTrackStreamingLocation(TestTrackIds.Create("feedback-track-rejected")),
            LookupPriorityBand.High,
            "Blocked",
            new DateTimeOffset(2026, 7, 19, 11, 2, 30, TimeSpan.Zero));

    public static WorkIgnored CreateIgnored() =>
        new(
            Work.EnrichTrackStreamingLocation(TestTrackIds.Create("feedback-track-ignored")),
            LookupPriorityBand.Low,
            NextEligibleAt: null,
            EstimatedRetryAfterSeconds: null,
            EarliestExpectedCompletionAt: new DateTimeOffset(2026, 7, 19, 11, 4, 0, TimeSpan.Zero),
            Reason: "Already planned",
            IgnoredAt: new DateTimeOffset(2026, 7, 19, 11, 2, 45, TimeSpan.Zero));

    public static WorkAttemptFailed CreateAttemptFailed() =>
        new(
            Work.EnrichTrackStreamingLocation(TestTrackIds.Create("feedback-track-attempt-failed")),
            "Provider timeout",
            new DateTimeOffset(2026, 7, 19, 11, 3, 0, TimeSpan.Zero));

    public sealed class StoreDiscoveryFeedbackPortFake : IStoreDiscoveryFeedbackPort
    {
        public object? StoredEvent { get; private set; }

        public Task StoreAsync(WorkRequested @event, CancellationToken cancellationToken)
        {
            StoredEvent = @event;
            return Task.CompletedTask;
        }

        public Task StoreAsync(WorkScheduled @event, CancellationToken cancellationToken)
        {
            StoredEvent = @event;
            return Task.CompletedTask;
        }

        public Task StoreAsync(WorkDeferred @event, CancellationToken cancellationToken)
        {
            StoredEvent = @event;
            return Task.CompletedTask;
        }

        public Task StoreAsync(WorkCompleted @event, CancellationToken cancellationToken)
        {
            StoredEvent = @event;
            return Task.CompletedTask;
        }

        public Task StoreAsync(WorkRejected @event, CancellationToken cancellationToken)
        {
            StoredEvent = @event;
            return Task.CompletedTask;
        }

        public Task StoreAsync(WorkIgnored @event, CancellationToken cancellationToken)
        {
            StoredEvent = @event;
            return Task.CompletedTask;
        }

        public Task StoreAsync(WorkAttemptFailed @event, CancellationToken cancellationToken)
        {
            StoredEvent = @event;
            return Task.CompletedTask;
        }
    }
}
