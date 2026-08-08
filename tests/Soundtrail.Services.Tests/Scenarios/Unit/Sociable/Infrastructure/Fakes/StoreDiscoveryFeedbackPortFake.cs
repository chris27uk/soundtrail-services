using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Events;
using Soundtrail.Services.Api.Shared.Contract;
using Soundtrail.Services.Internal.Projector.Features.OnWorkFeedbackChanged.Adapters;

namespace Soundtrail.Services.Tests.Unit.Sociable.Infrastructure.Fakes;

internal sealed class StoreDiscoveryFeedbackPortFake : IStoreDiscoveryFeedbackPort
{
    private readonly Dictionary<string, DiscoveryFeedbackResponse> feedback = new(StringComparer.Ordinal);

    public object? StoredEvent { get; private set; }

    public DiscoveryFeedbackResponse? Read(string targetId) => this.feedback.GetValueOrDefault(targetId);

    public void Seed(string targetId, DiscoveryFeedbackResponse response) =>
        this.feedback[targetId] = response;

    public Task StoreAsync(WorkRequested @event, CancellationToken cancellationToken)
    {
        StoredEvent = @event;
        return Store(@event.Target, "requested", @event.Priority, null, null, string.Empty, @event.RequestedAt);
    }

    public Task StoreAsync(WorkScheduled @event, CancellationToken cancellationToken)
    {
        StoredEvent = @event;
        return Store(@event.Target, "scheduled", @event.Priority, @event.NextEligibleAt, @event.EarliestExpectedCompletionAt, @event.Reason, @event.ScheduledAt);
    }

    public Task StoreAsync(WorkDeferred @event, CancellationToken cancellationToken)
    {
        StoredEvent = @event;
        return Store(@event.Target, "deferred", @event.Priority, @event.NextEligibleAt, null, @event.Reason, @event.DeferredAt);
    }

    public Task StoreAsync(WorkCompleted @event, CancellationToken cancellationToken)
    {
        StoredEvent = @event;
        return Store(@event.Target, "completed", @event.Priority, null, null, @event.Reason, @event.CompletedAt);
    }

    public Task StoreAsync(WorkRejected @event, CancellationToken cancellationToken)
    {
        StoredEvent = @event;
        return Store(@event.Target, "rejected", @event.Priority, null, null, @event.Reason, @event.RejectedAt);
    }

    public Task StoreAsync(WorkIgnored @event, CancellationToken cancellationToken)
    {
        StoredEvent = @event;
        return Store(@event.Target, "ignored", @event.Priority, @event.NextEligibleAt, @event.EarliestExpectedCompletionAt, @event.Reason, @event.IgnoredAt);
    }

    public Task StoreAsync(WorkAttemptFailed @event, CancellationToken cancellationToken)
    {
        StoredEvent = @event;
        var existing = Read(@event.Target.NormalisedIdentifier);
        if (existing?.Status == "completed")
        {
            return Task.CompletedTask;
        }

        this.feedback[@event.Target.NormalisedIdentifier] = (existing ?? new DiscoveryFeedbackResponse(
            string.Empty,
            LookupPriorityBand.Low,
            null,
            null,
            string.Empty,
            @event.FailedAt)) with
        {
            Status = "attempt-failed",
            Reason = @event.Reason,
            UpdatedAtUtc = @event.FailedAt
        };
        return Task.CompletedTask;
    }

    private Task Store(
        EnrichmentTarget target,
        string status,
        LookupPriorityBand priority,
        DateTimeOffset? nextEligibleAt,
        DateTimeOffset? earliestExpectedCompletionAt,
        string reason,
        DateTimeOffset updatedAt)
    {
        this.feedback[target.NormalisedIdentifier] = new DiscoveryFeedbackResponse(
            status,
            priority,
            nextEligibleAt,
            earliestExpectedCompletionAt,
            reason,
            updatedAt);
        return Task.CompletedTask;
    }
}