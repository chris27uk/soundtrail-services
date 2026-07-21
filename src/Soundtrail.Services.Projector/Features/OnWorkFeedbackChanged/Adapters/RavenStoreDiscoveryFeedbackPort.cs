using Raven.Client.Documents;
using Soundtrail.Contracts.Persistence;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery.Events;

namespace Soundtrail.Services.Internal.Projector.Features.OnWorkFeedbackChanged.Adapters;

public sealed class RavenStoreDiscoveryFeedbackPort(
    IDocumentStore documentStore) : IStoreDiscoveryFeedbackPort
{
    public Task StoreAsync(WorkRequested @event, CancellationToken cancellationToken) =>
        UpsertAsync(
            @event.Target.NormalisedIdentifier,
            "requested",
            @event.Priority,
            nextEligibleAtUtc: null,
            earliestExpectedCompletionAtUtc: null,
            reason: string.Empty,
            updatedAtUtc: @event.RequestedAt,
            cancellationToken);

    public Task StoreAsync(WorkScheduled @event, CancellationToken cancellationToken) =>
        UpsertAsync(
            @event.Target.NormalisedIdentifier,
            "scheduled",
            @event.Priority,
            @event.NextEligibleAt,
            @event.EarliestExpectedCompletionAt,
            @event.Reason,
            @event.ScheduledAt,
            cancellationToken);

    public Task StoreAsync(WorkDeferred @event, CancellationToken cancellationToken) =>
        UpsertAsync(
            @event.Target.NormalisedIdentifier,
            "deferred",
            @event.Priority,
            @event.NextEligibleAt,
            earliestExpectedCompletionAtUtc: null,
            @event.Reason,
            @event.DeferredAt,
            cancellationToken);

    public Task StoreAsync(WorkCompleted @event, CancellationToken cancellationToken) =>
        UpsertAsync(
            @event.Target.NormalisedIdentifier,
            "completed",
            @event.Priority,
            nextEligibleAtUtc: null,
            earliestExpectedCompletionAtUtc: null,
            @event.Reason,
            @event.CompletedAt,
            cancellationToken);

    public Task StoreAsync(WorkRejected @event, CancellationToken cancellationToken) =>
        UpsertAsync(
            @event.Target.NormalisedIdentifier,
            "rejected",
            @event.Priority,
            nextEligibleAtUtc: null,
            earliestExpectedCompletionAtUtc: null,
            @event.Reason,
            @event.RejectedAt,
            cancellationToken);

    public Task StoreAsync(WorkIgnored @event, CancellationToken cancellationToken) =>
        UpsertAsync(
            @event.Target.NormalisedIdentifier,
            "ignored",
            @event.Priority,
            @event.NextEligibleAt,
            @event.EarliestExpectedCompletionAt,
            @event.Reason,
            @event.IgnoredAt,
            cancellationToken);

    public async Task StoreAsync(WorkAttemptFailed @event, CancellationToken cancellationToken)
    {
        using var session = documentStore.OpenAsyncSession();
        var id = CatalogDiscoveryFeedbackRecordDto.GetDocumentId(@event.Target.NormalisedIdentifier);
        var record = await session.LoadAsync<CatalogDiscoveryFeedbackRecordDto>(id, cancellationToken)
            ?? CreateRecord(@event.Target.NormalisedIdentifier);

        record.Status = "attempt-failed";
        record.Reason = @event.Reason;
        record.UpdatedAtUtc = @event.FailedAt;

        await session.StoreAsync(record, cancellationToken);
        await session.SaveChangesAsync(cancellationToken);
    }

    private async Task UpsertAsync(
        string targetId,
        string status,
        LookupPriorityBand priority,
        DateTimeOffset? nextEligibleAtUtc,
        DateTimeOffset? earliestExpectedCompletionAtUtc,
        string reason,
        DateTimeOffset updatedAtUtc,
        CancellationToken cancellationToken)
    {
        using var session = documentStore.OpenAsyncSession();
        var id = CatalogDiscoveryFeedbackRecordDto.GetDocumentId(targetId);
        var record = await session.LoadAsync<CatalogDiscoveryFeedbackRecordDto>(id, cancellationToken)
            ?? CreateRecord(targetId);

        record.Status = status;
        record.Priority = priority.ToString();
        record.NextEligibleAtUtc = nextEligibleAtUtc;
        record.EarliestExpectedCompletionAtUtc = earliestExpectedCompletionAtUtc;
        record.Reason = reason;
        record.UpdatedAtUtc = updatedAtUtc;

        await session.StoreAsync(record, cancellationToken);
        await session.SaveChangesAsync(cancellationToken);
    }

    private static CatalogDiscoveryFeedbackRecordDto CreateRecord(string targetId) =>
        new()
        {
            Id = CatalogDiscoveryFeedbackRecordDto.GetDocumentId(targetId),
            TargetId = targetId,
            Priority = LookupPriorityBand.Low.ToString()
        };
}
