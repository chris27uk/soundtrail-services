using Raven.Client.Documents.Session;
using Soundtrail.Contracts;
using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Domain.Discovery;
using System.Text.Json;

namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.ProjectDiscoveryLifecycle.Adapters;

public sealed class DiscoveryLifecycleProjectionApplier
{
    public async Task ApplyStoredEventAsync(
        DiscoveryQueryStoredEventRecordDto storedEvent,
        IAsyncDocumentSession session,
        CancellationToken cancellationToken)
    {
        var checkpoint = await session.LoadAsync<DiscoveryLifecycleProjectionCheckpointDocument>(
                             DiscoveryLifecycleProjectionCheckpointDocument.GetDocumentId(storedEvent.Criteria),
                             cancellationToken)
                         ?? new DiscoveryLifecycleProjectionCheckpointDocument
                         {
                             Id = DiscoveryLifecycleProjectionCheckpointDocument.GetDocumentId(storedEvent.Criteria),
                             Criteria = storedEvent.Criteria
                         };

        if (checkpoint.LastAppliedVersion >= storedEvent.Version)
        {
            return;
        }

        var status = await session.LoadAsync<CatalogSearchStatusRecordDto>(
                         CatalogSearchStatusRecordDto.GetDocumentId(storedEvent.Criteria),
                         cancellationToken)
                     ?? new CatalogSearchStatusRecordDto
                     {
                         Id = CatalogSearchStatusRecordDto.GetDocumentId(storedEvent.Criteria),
                         Criteria = storedEvent.Criteria
                     };

        switch (storedEvent.EventType)
        {
            case "DiscoveryRequested":
                ApplyRequested(status, Deserialize<DiscoveryRequestedEventDataRecordDto>(storedEvent));
                break;
            case "DiscoveryPlanned":
                ApplyPlanned(status, Deserialize<DiscoveryPlannedEventDataRecordDto>(storedEvent));
                break;
            case "DiscoveryDeferred":
                ApplyDeferred(status, Deserialize<DiscoveryDeferredEventDataRecordDto>(storedEvent));
                break;
            case "DiscoveryRejected":
                ApplyRejected(status, Deserialize<DiscoveryRejectedEventDataRecordDto>(storedEvent));
                break;
            case "DiscoveryFailed":
                ApplyFailed(status, Deserialize<DiscoveryFailedEventDataRecordDto>(storedEvent));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(storedEvent.EventType), storedEvent.EventType, "Unknown discovery lifecycle event type.");
        }

        checkpoint.LastAppliedVersion = storedEvent.Version;
        checkpoint.UpdatedAt = storedEvent.OccurredAtUtc;
        await session.StoreAsync(checkpoint, cancellationToken);
        await session.StoreAsync(status, cancellationToken);
    }

    private static void ApplyRequested(
        CatalogSearchStatusRecordDto status,
        DiscoveryRequestedEventDataRecordDto data)
    {
        status.Status = CatalogSearchLifecycleStatus.Requested.ToString();
        status.Priority = string.Empty;
        status.WillBeLookedUp = true;
        status.EstimatedRetryAfterSeconds = null;
        status.EarliestExpectedCompletionAt = null;
        status.Reason = "Discovery requested";
        status.UpdatedAt = data.RequestedAtUtc;
    }

    private static void ApplyPlanned(
        CatalogSearchStatusRecordDto status,
        DiscoveryPlannedEventDataRecordDto data)
    {
        status.Status = CatalogSearchLifecycleStatus.Planned.ToString();
        status.Priority = data.Priority;
        status.WillBeLookedUp = data.WillBeLookedUp;
        status.EstimatedRetryAfterSeconds = data.EstimatedRetryAfterSeconds;
        status.EarliestExpectedCompletionAt = data.EarliestExpectedCompletionAt;
        status.Reason = data.Reason;
        status.UpdatedAt = data.PlannedAtUtc;
    }

    private static void ApplyDeferred(
        CatalogSearchStatusRecordDto status,
        DiscoveryDeferredEventDataRecordDto data)
    {
        status.Status = CatalogSearchLifecycleStatus.Deferred.ToString();
        status.Priority = string.Empty;
        status.WillBeLookedUp = data.WillBeLookedUp;
        status.EstimatedRetryAfterSeconds = data.EstimatedRetryAfterSeconds;
        status.EarliestExpectedCompletionAt = data.EarliestExpectedCompletionAt;
        status.Reason = data.Reason;
        status.UpdatedAt = data.DeferredAtUtc;
    }

    private static void ApplyRejected(
        CatalogSearchStatusRecordDto status,
        DiscoveryRejectedEventDataRecordDto data)
    {
        status.Status = CatalogSearchLifecycleStatus.Rejected.ToString();
        status.Priority = string.Empty;
        status.WillBeLookedUp = data.WillBeLookedUp;
        status.EstimatedRetryAfterSeconds = null;
        status.EarliestExpectedCompletionAt = null;
        status.Reason = data.Reason;
        status.UpdatedAt = data.RejectedAtUtc;
    }

    private static void ApplyFailed(
        CatalogSearchStatusRecordDto status,
        DiscoveryFailedEventDataRecordDto data)
    {
        status.Status = CatalogSearchLifecycleStatus.Failed.ToString();
        status.Priority = string.Empty;
        status.WillBeLookedUp = data.WillBeLookedUp;
        status.EstimatedRetryAfterSeconds = null;
        status.EarliestExpectedCompletionAt = null;
        status.Reason = data.Reason;
        status.UpdatedAt = data.FailedAtUtc;
    }

    private static T Deserialize<T>(DiscoveryQueryStoredEventRecordDto storedEvent) where T : class =>
        JsonSerializer.Deserialize<T>(storedEvent.Data)
        ?? throw new InvalidOperationException($"Unable to deserialize {storedEvent.EventType}.");
}
