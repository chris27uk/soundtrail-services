using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Events;
using Soundtrail.Domain.Model;
using System.Text.Json;

namespace Soundtrail.Services.Api.Features.Search.SearchCatalog.Adapters.Mappers;

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
                Data = JsonSerializer.Serialize(new DiscoveryRequestedEventDataRecordDto(
                    requested.Criteria.Value,
                    requested.Query.Value,
                    requested.TrustLevel,
                    requested.RiskScore,
                    requested.RequestedAt,
                    requested.CorrelationId.Value)),
                OccurredAtUtc = requested.RequestedAt,
                CorrelationId = requested.CorrelationId.Value
            },
            DiscoveryPlanned planned => new DiscoveryQueryStoredEventRecordDto
            {
                Id = DiscoveryQueryStoredEventRecordDto.GetDocumentId(criteria.Value, version),
                Criteria = criteria.Value,
                Version = version,
                EventType = nameof(DiscoveryPlanned),
                Data = JsonSerializer.Serialize(new DiscoveryPlannedEventDataRecordDto(
                    planned.Criteria.Value,
                    planned.Priority.ToString(),
                    planned.WillBeLookedUp,
                    planned.EstimatedRetryAfterSeconds,
                    planned.EarliestExpectedCompletionAt,
                    planned.Reason,
                    planned.PlannedAt)),
                OccurredAtUtc = planned.PlannedAt
            },
            DiscoveryDeferred deferred => new DiscoveryQueryStoredEventRecordDto
            {
                Id = DiscoveryQueryStoredEventRecordDto.GetDocumentId(criteria.Value, version),
                Criteria = criteria.Value,
                Version = version,
                EventType = nameof(DiscoveryDeferred),
                Data = JsonSerializer.Serialize(new DiscoveryDeferredEventDataRecordDto(
                    deferred.Criteria.Value,
                    deferred.WillBeLookedUp,
                    deferred.EstimatedRetryAfterSeconds,
                    deferred.EarliestExpectedCompletionAt,
                    deferred.Reason,
                    deferred.DeferredAt)),
                OccurredAtUtc = deferred.DeferredAt
            },
            DiscoveryRejected rejected => new DiscoveryQueryStoredEventRecordDto
            {
                Id = DiscoveryQueryStoredEventRecordDto.GetDocumentId(criteria.Value, version),
                Criteria = criteria.Value,
                Version = version,
                EventType = nameof(DiscoveryRejected),
                Data = JsonSerializer.Serialize(new DiscoveryRejectedEventDataRecordDto(
                    rejected.Criteria.Value,
                    rejected.WillBeLookedUp,
                    rejected.Reason,
                    rejected.RejectedAt)),
                OccurredAtUtc = rejected.RejectedAt
            },
            DiscoveryFailed failed => new DiscoveryQueryStoredEventRecordDto
            {
                Id = DiscoveryQueryStoredEventRecordDto.GetDocumentId(criteria.Value, version),
                Criteria = criteria.Value,
                Version = version,
                EventType = nameof(DiscoveryFailed),
                Data = JsonSerializer.Serialize(new DiscoveryFailedEventDataRecordDto(
                    failed.Criteria.Value,
                    failed.WillBeLookedUp,
                    failed.Reason,
                    failed.FailedAt)),
                OccurredAtUtc = failed.FailedAt
            },
            _ => throw new ArgumentOutOfRangeException(nameof(@event), @event, "Unknown discovery event.")
        };

    private static DiscoveryRequested ToDiscoveryRequested(DiscoveryQueryStoredEventRecordDto dto)
    {
        var data = JsonSerializer.Deserialize<DiscoveryRequestedEventDataRecordDto>(dto.Data)
            ?? throw new InvalidOperationException("Unable to deserialize discovery requested event data.");
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
        var data = JsonSerializer.Deserialize<DiscoveryPlannedEventDataRecordDto>(dto.Data)
            ?? throw new InvalidOperationException("Unable to deserialize discovery planned event data.");
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
        var data = JsonSerializer.Deserialize<DiscoveryDeferredEventDataRecordDto>(dto.Data)
            ?? throw new InvalidOperationException("Unable to deserialize discovery deferred event data.");
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
        var data = JsonSerializer.Deserialize<DiscoveryRejectedEventDataRecordDto>(dto.Data)
            ?? throw new InvalidOperationException("Unable to deserialize discovery rejected event data.");
        return new DiscoveryRejected(
            CatalogSearchCriteria.From(data.Criteria),
            data.WillBeLookedUp,
            data.Reason,
            data.RejectedAtUtc);
    }

    private static DiscoveryFailed ToDiscoveryFailed(DiscoveryQueryStoredEventRecordDto dto)
    {
        var data = JsonSerializer.Deserialize<DiscoveryFailedEventDataRecordDto>(dto.Data)
            ?? throw new InvalidOperationException("Unable to deserialize discovery failed event data.");
        return new DiscoveryFailed(
            CatalogSearchCriteria.From(data.Criteria),
            data.WillBeLookedUp,
            data.Reason,
            data.FailedAtUtc);
    }
}
