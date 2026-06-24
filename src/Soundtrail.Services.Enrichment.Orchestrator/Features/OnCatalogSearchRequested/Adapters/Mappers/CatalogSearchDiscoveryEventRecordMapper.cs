using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Events;
using Soundtrail.Domain.Model;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnCatalogSearchRequested.Adapters.Mappers;

internal static class CatalogSearchDiscoveryEventRecordMapper
{
    public static IReadOnlyList<DiscoveryQueryStoredEventRecordDto> ToStoredEvents(
        CatalogSearchCriteria criteria,
        IReadOnlyCollection<IDomainEvent> events,
        int startingVersion) =>
        events.Select((@event, index) => ToStoredEvent(criteria, @event, startingVersion + index + 1))
            .ToArray();

    public static IDomainEvent ToDomainEvent(DiscoveryQueryStoredEventRecordDto dto) =>
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

    public static DateTimeOffset GetOccurredAtUtc(IDomainEvent @event) =>
        @event switch
        {
            DiscoveryRequested requested => requested.RequestedAt,
            DiscoveryPlanned planned => planned.PlannedAt,
            DiscoveryDeferred deferred => deferred.DeferredAt,
            DiscoveryRejected rejected => rejected.RejectedAt,
            DiscoveryFailed failed => failed.FailedAt,
            DiscoveryStarted started => started.StartedAt,
            DiscoveryCompleted completed => completed.CompletedAt,
            _ => throw new ArgumentOutOfRangeException(nameof(@event), @event, "Unknown discovery event.")
        };

    private static DiscoveryQueryStoredEventRecordDto ToStoredEvent(
        CatalogSearchCriteria criteria,
        IDomainEvent @event,
        int version) =>
        @event switch
        {
            DiscoveryRequested requested => new DiscoveryQueryStoredEventRecordDto
            {
                Id = DiscoveryQueryStoredEventRecordDto.GetDocumentId(criteria.Value, version),
                Criteria = criteria.Value,
                Version = version,
                EventType = nameof(DiscoveryRequested),
                DiscoveryRequested = new DiscoveryRequestedEventDataRecordDto(
                    requested.Criteria.Value,
                    requested.Query.Value,
                    requested.TrustLevel,
                    requested.RiskScore,
                    requested.RequestedAt,
                    requested.CorrelationId.Value),
                OccurredAtUtc = requested.RequestedAt,
                CorrelationId = requested.CorrelationId.Value
            },
            DiscoveryPlanned planned => new DiscoveryQueryStoredEventRecordDto
            {
                Id = DiscoveryQueryStoredEventRecordDto.GetDocumentId(criteria.Value, version),
                Criteria = criteria.Value,
                Version = version,
                EventType = nameof(DiscoveryPlanned),
                DiscoveryPlanned = new DiscoveryPlannedEventDataRecordDto(
                    planned.Criteria.Value,
                    planned.Priority.ToString(),
                    planned.WillBeLookedUp,
                    planned.EstimatedRetryAfterSeconds,
                    planned.EarliestExpectedCompletionAt,
                    planned.Reason,
                    planned.PlannedAt),
                OccurredAtUtc = planned.PlannedAt
            },
            DiscoveryDeferred deferred => new DiscoveryQueryStoredEventRecordDto
            {
                Id = DiscoveryQueryStoredEventRecordDto.GetDocumentId(criteria.Value, version),
                Criteria = criteria.Value,
                Version = version,
                EventType = nameof(DiscoveryDeferred),
                DiscoveryDeferred = new DiscoveryDeferredEventDataRecordDto(
                    deferred.Criteria.Value,
                    deferred.WillBeLookedUp,
                    deferred.EstimatedRetryAfterSeconds,
                    deferred.EarliestExpectedCompletionAt,
                    deferred.Reason,
                    deferred.DeferredAt),
                OccurredAtUtc = deferred.DeferredAt
            },
            DiscoveryRejected rejected => new DiscoveryQueryStoredEventRecordDto
            {
                Id = DiscoveryQueryStoredEventRecordDto.GetDocumentId(criteria.Value, version),
                Criteria = criteria.Value,
                Version = version,
                EventType = nameof(DiscoveryRejected),
                DiscoveryRejected = new DiscoveryRejectedEventDataRecordDto(
                    rejected.Criteria.Value,
                    rejected.WillBeLookedUp,
                    rejected.Reason,
                    rejected.RejectedAt),
                OccurredAtUtc = rejected.RejectedAt
            },
            DiscoveryFailed failed => new DiscoveryQueryStoredEventRecordDto
            {
                Id = DiscoveryQueryStoredEventRecordDto.GetDocumentId(criteria.Value, version),
                Criteria = criteria.Value,
                Version = version,
                EventType = nameof(DiscoveryFailed),
                DiscoveryFailed = new DiscoveryFailedEventDataRecordDto(
                    failed.Criteria.Value,
                    failed.WillBeLookedUp,
                    failed.Reason,
                    failed.FailedAt),
                OccurredAtUtc = failed.FailedAt
            },
            DiscoveryStarted started => new DiscoveryQueryStoredEventRecordDto
            {
                Id = DiscoveryQueryStoredEventRecordDto.GetDocumentId(criteria.Value, version),
                Criteria = criteria.Value,
                Version = version,
                EventType = nameof(DiscoveryStarted),
                DiscoveryStarted = new DiscoveryStartedEventDataRecordDto(
                    started.Criteria.Value,
                    started.Priority.ToString(),
                    started.WillBeLookedUp,
                    started.Reason,
                    started.StartedAt),
                OccurredAtUtc = started.StartedAt
            },
            DiscoveryCompleted completed => new DiscoveryQueryStoredEventRecordDto
            {
                Id = DiscoveryQueryStoredEventRecordDto.GetDocumentId(criteria.Value, version),
                Criteria = criteria.Value,
                Version = version,
                EventType = nameof(DiscoveryCompleted),
                DiscoveryCompleted = new DiscoveryCompletedEventDataRecordDto(
                    completed.Criteria.Value,
                    completed.Priority.ToString(),
                    completed.WillBeLookedUp,
                    completed.Reason,
                    completed.CompletedAt),
                OccurredAtUtc = completed.CompletedAt
            },
            _ => throw new ArgumentOutOfRangeException(nameof(@event), @event, "Unknown discovery event.")
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
