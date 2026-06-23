using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Events;
using Soundtrail.Domain.Model;

namespace Soundtrail.Services.Internal.Projector.Features.ProjectDiscoveryLifecycle.Adapters;

public static class DiscoveryQueryStoredEventRecordMapper
{
    public static VersionedCatalogSearchDiscoveryEvent ToDomainEvent(this DiscoveryQueryStoredEventRecordDto dto) =>
        new(dto.Version, ToDomainEventData(dto));

    private static IDomainEvent ToDomainEventData(DiscoveryQueryStoredEventRecordDto dto) =>
        dto.EventType switch
        {
            nameof(DiscoveryRequested) => ToDiscoveryRequested(dto),
            nameof(DiscoveryPlanned) => ToDiscoveryPlanned(dto),
            nameof(DiscoveryDeferred) => ToDiscoveryDeferred(dto),
            nameof(DiscoveryRejected) => ToDiscoveryRejected(dto),
            nameof(DiscoveryFailed) => ToDiscoveryFailed(dto),
            nameof(DiscoveryStarted) => ToDiscoveryStarted(dto),
            nameof(DiscoveryCompleted) => ToDiscoveryCompleted(dto),
            _ => throw new ArgumentOutOfRangeException(nameof(dto.EventType), dto.EventType, "Unknown discovery event type.")
        };

    private static DiscoveryRequested ToDiscoveryRequested(DiscoveryQueryStoredEventRecordDto dto)
    {
        var data = dto.DiscoveryRequested
            ?? throw new InvalidOperationException("Missing discovery requested event data.");
        return new DiscoveryRequested(
            CatalogSearchCriteria.From(data.Criteria),
            NormalizedSearchQuery.FromText(data.Query),
            data.TrustLevel,
            data.RiskScore,
            data.RequestedAtUtc,
            CorrelationId.From(data.CorrelationId));
    }

    private static DiscoveryPlanned ToDiscoveryPlanned(DiscoveryQueryStoredEventRecordDto dto)
    {
        var data = dto.DiscoveryPlanned
            ?? throw new InvalidOperationException("Missing discovery planned event data.");
        return new DiscoveryPlanned(
            CatalogSearchCriteria.From(data.Criteria),
            Enum.Parse<LookupPriorityBand>(data.Priority, ignoreCase: true),
            data.WillBeLookedUp,
            data.EstimatedRetryAfterSeconds,
            data.EarliestExpectedCompletionAt,
            data.Reason,
            data.PlannedAtUtc);
    }

    private static DiscoveryDeferred ToDiscoveryDeferred(DiscoveryQueryStoredEventRecordDto dto)
    {
        var data = dto.DiscoveryDeferred
            ?? throw new InvalidOperationException("Missing discovery deferred event data.");
        return new DiscoveryDeferred(
            CatalogSearchCriteria.From(data.Criteria),
            data.WillBeLookedUp,
            data.EstimatedRetryAfterSeconds,
            data.EarliestExpectedCompletionAt,
            data.Reason,
            data.DeferredAtUtc);
    }

    private static DiscoveryRejected ToDiscoveryRejected(DiscoveryQueryStoredEventRecordDto dto)
    {
        var data = dto.DiscoveryRejected
            ?? throw new InvalidOperationException("Missing discovery rejected event data.");
        return new DiscoveryRejected(
            CatalogSearchCriteria.From(data.Criteria),
            data.WillBeLookedUp,
            data.Reason,
            data.RejectedAtUtc);
    }

    private static DiscoveryFailed ToDiscoveryFailed(DiscoveryQueryStoredEventRecordDto dto)
    {
        var data = dto.DiscoveryFailed
            ?? throw new InvalidOperationException("Missing discovery failed event data.");
        return new DiscoveryFailed(
            CatalogSearchCriteria.From(data.Criteria),
            data.WillBeLookedUp,
            data.Reason,
            data.FailedAtUtc);
    }

    private static DiscoveryStarted ToDiscoveryStarted(DiscoveryQueryStoredEventRecordDto dto)
    {
        var data = dto.DiscoveryStarted
            ?? throw new InvalidOperationException("Missing discovery started event data.");
        return new DiscoveryStarted(
            CatalogSearchCriteria.From(data.Criteria),
            Enum.Parse<LookupPriorityBand>(data.Priority, ignoreCase: true),
            data.WillBeLookedUp,
            data.Reason,
            data.StartedAtUtc);
    }

    private static DiscoveryCompleted ToDiscoveryCompleted(DiscoveryQueryStoredEventRecordDto dto)
    {
        var data = dto.DiscoveryCompleted
            ?? throw new InvalidOperationException("Missing discovery completed event data.");
        return new DiscoveryCompleted(
            CatalogSearchCriteria.From(data.Criteria),
            Enum.Parse<LookupPriorityBand>(data.Priority, ignoreCase: true),
            data.WillBeLookedUp,
            data.Reason,
            data.CompletedAtUtc);
    }
}
