using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Events;

namespace Soundtrail.Services.Enrichment.Orchestrator.Infrastructure.Raven.CatalogDiscoveryWork;

internal static class CatalogDiscoveryWorkEventRecordMapper
{
    public static IReadOnlyList<CatalogDiscoveryWorkStoredEventRecordDto> ToStoredEvents(
        MusicCatalogId musicCatalogId,
        IReadOnlyCollection<IDomainEvent> events,
        int startingVersion) =>
        events.Select((@event, index) => ToStoredEvent(musicCatalogId, @event, startingVersion + index + 1))
            .ToArray();

    public static IDomainEvent ToDomainEvent(CatalogDiscoveryWorkStoredEventRecordDto dto) =>
        dto.EventType switch
        {
            nameof(CatalogDiscoveryWorkRequested) => ToCatalogDiscoveryWorkRequested(dto),
            nameof(CatalogDiscoveryWorkDeferred) => ToCatalogDiscoveryWorkDeferred(dto),
            nameof(CatalogDiscoveryWorkIgnored) => ToCatalogDiscoveryWorkIgnored(dto),
            nameof(CatalogDiscoveryWorkScheduled) => ToCatalogDiscoveryWorkScheduled(dto),
            _ => throw new ArgumentOutOfRangeException(nameof(dto.EventType), dto.EventType, "Unknown catalog discovery work event type.")
        };

    public static DateTimeOffset GetOccurredAtUtc(IDomainEvent @event) =>
        @event switch
        {
            CatalogDiscoveryWorkRequested requested => requested.RequestedAt,
            CatalogDiscoveryWorkDeferred deferred => deferred.DeferredAt,
            CatalogDiscoveryWorkIgnored ignored => ignored.IgnoredAt,
            CatalogDiscoveryWorkScheduled scheduled => scheduled.ScheduledAt,
            _ => throw new ArgumentOutOfRangeException(nameof(@event), @event, "Unknown catalog discovery work event.")
        };

    public static CatalogDiscoveryWorkSummary ToSummary(CatalogDiscoveryWorkSummaryRecordDto document) =>
        new(
            MusicCatalogId.From(document.MusicCatalogId),
            document.RequestCount,
            document.HighestTrustLevelSeen,
            document.RiskScore,
            Enum.Parse<CatalogDiscoveryWorkStatus>(document.Status, ignoreCase: true),
            document.NextEligibleAt,
            document.Priority is null ? null : Enum.Parse<LookupPriorityBand>(document.Priority, ignoreCase: true),
            document.Reason);

    public static void Apply(
        CatalogDiscoveryWorkSummaryRecordDto document,
        IReadOnlyCollection<IDomainEvent> events)
    {
        foreach (var @event in events)
        {
            switch (@event)
            {
                case CatalogDiscoveryWorkRequested requested:
                    document.MusicCatalogId = requested.MusicCatalogId.Value;
                    document.RequestCount += 1;
                    document.HighestTrustLevelSeen = Math.Max(document.HighestTrustLevelSeen, requested.TrustLevel);
                    document.RiskScore = Math.Max(document.RiskScore, requested.RiskScore);
                    document.Status = (requested.RiskScore >= 90
                        ? CatalogDiscoveryWorkStatus.Ignored
                        : CatalogDiscoveryWorkStatus.Pending).ToString();
                    document.Reason = null;
                    document.UpdatedAtUtc = requested.RequestedAt;
                    break;
                case CatalogDiscoveryWorkDeferred deferred:
                    document.MusicCatalogId = deferred.MusicCatalogId.Value;
                    document.Status = CatalogDiscoveryWorkStatus.Pending.ToString();
                    document.NextEligibleAt = deferred.NextEligibleAt;
                    document.Priority = null;
                    document.Reason = deferred.Reason;
                    document.UpdatedAtUtc = deferred.DeferredAt;
                    break;
                case CatalogDiscoveryWorkIgnored ignored:
                    document.MusicCatalogId = ignored.MusicCatalogId.Value;
                    document.Status = CatalogDiscoveryWorkStatus.Ignored.ToString();
                    document.NextEligibleAt = ignored.NextEligibleAt;
                    document.Priority = null;
                    document.Reason = ignored.Reason;
                    document.UpdatedAtUtc = ignored.IgnoredAt;
                    break;
                case CatalogDiscoveryWorkScheduled scheduled:
                    document.MusicCatalogId = scheduled.MusicCatalogId.Value;
                    document.Status = CatalogDiscoveryWorkStatus.Pending.ToString();
                    document.NextEligibleAt = null;
                    document.Priority = scheduled.Priority.ToString();
                    document.Reason = scheduled.Reason;
                    document.UpdatedAtUtc = scheduled.ScheduledAt;
                    break;
            }
        }
    }

    private static CatalogDiscoveryWorkStoredEventRecordDto ToStoredEvent(
        MusicCatalogId musicCatalogId,
        IDomainEvent @event,
        int version) =>
        @event switch
        {
            CatalogDiscoveryWorkRequested requested => new CatalogDiscoveryWorkStoredEventRecordDto
            {
                Id = CatalogDiscoveryWorkStoredEventRecordDto.GetDocumentId(musicCatalogId.Value, version),
                MusicCatalogId = musicCatalogId.Value,
                Version = version,
                EventType = nameof(CatalogDiscoveryWorkRequested),
                CatalogDiscoveryWorkRequested = new CatalogDiscoveryWorkRequestedEventDataRecordDto(
                    requested.MusicCatalogId.Value,
                    requested.TrustLevel,
                    requested.RiskScore,
                    requested.RequestedAt),
                OccurredAtUtc = requested.RequestedAt
            },
            CatalogDiscoveryWorkDeferred deferred => new CatalogDiscoveryWorkStoredEventRecordDto
            {
                Id = CatalogDiscoveryWorkStoredEventRecordDto.GetDocumentId(musicCatalogId.Value, version),
                MusicCatalogId = musicCatalogId.Value,
                Version = version,
                EventType = nameof(CatalogDiscoveryWorkDeferred),
                CatalogDiscoveryWorkDeferred = new CatalogDiscoveryWorkDeferredEventDataRecordDto(
                    deferred.MusicCatalogId.Value,
                    deferred.NextEligibleAt,
                    deferred.Reason,
                    deferred.DeferredAt),
                OccurredAtUtc = deferred.DeferredAt
            },
            CatalogDiscoveryWorkIgnored ignored => new CatalogDiscoveryWorkStoredEventRecordDto
            {
                Id = CatalogDiscoveryWorkStoredEventRecordDto.GetDocumentId(musicCatalogId.Value, version),
                MusicCatalogId = musicCatalogId.Value,
                Version = version,
                EventType = nameof(CatalogDiscoveryWorkIgnored),
                CatalogDiscoveryWorkIgnored = new CatalogDiscoveryWorkIgnoredEventDataRecordDto(
                    ignored.MusicCatalogId.Value,
                    ignored.NextEligibleAt,
                    ignored.Reason,
                    ignored.IgnoredAt),
                OccurredAtUtc = ignored.IgnoredAt
            },
            CatalogDiscoveryWorkScheduled scheduled => new CatalogDiscoveryWorkStoredEventRecordDto
            {
                Id = CatalogDiscoveryWorkStoredEventRecordDto.GetDocumentId(musicCatalogId.Value, version),
                MusicCatalogId = musicCatalogId.Value,
                Version = version,
                EventType = nameof(CatalogDiscoveryWorkScheduled),
                CatalogDiscoveryWorkScheduled = new CatalogDiscoveryWorkScheduledEventDataRecordDto(
                    scheduled.MusicCatalogId.Value,
                    scheduled.Priority.ToString(),
                    scheduled.NextEligibleAt,
                    scheduled.Reason,
                    scheduled.ScheduledAt),
                OccurredAtUtc = scheduled.ScheduledAt
            },
            _ => throw new ArgumentOutOfRangeException(nameof(@event), @event, "Unknown catalog discovery work event.")
        };

    private static CatalogDiscoveryWorkRequested ToCatalogDiscoveryWorkRequested(CatalogDiscoveryWorkStoredEventRecordDto dto)
    {
        var data = dto.CatalogDiscoveryWorkRequested
            ?? throw new InvalidOperationException("Missing catalog discovery work requested event data.");
        return new CatalogDiscoveryWorkRequested(
            MusicCatalogId.From(data.MusicCatalogId),
            data.TrustLevel,
            data.RiskScore,
            data.RequestedAtUtc);
    }

    private static CatalogDiscoveryWorkDeferred ToCatalogDiscoveryWorkDeferred(CatalogDiscoveryWorkStoredEventRecordDto dto)
    {
        var data = dto.CatalogDiscoveryWorkDeferred
            ?? throw new InvalidOperationException("Missing catalog discovery work deferred event data.");
        return new CatalogDiscoveryWorkDeferred(
            MusicCatalogId.From(data.MusicCatalogId),
            data.NextEligibleAtUtc,
            data.Reason,
            data.DeferredAtUtc);
    }

    private static CatalogDiscoveryWorkIgnored ToCatalogDiscoveryWorkIgnored(CatalogDiscoveryWorkStoredEventRecordDto dto)
    {
        var data = dto.CatalogDiscoveryWorkIgnored
            ?? throw new InvalidOperationException("Missing catalog discovery work ignored event data.");
        return new CatalogDiscoveryWorkIgnored(
            MusicCatalogId.From(data.MusicCatalogId),
            data.NextEligibleAtUtc,
            data.Reason,
            data.IgnoredAtUtc);
    }

    private static CatalogDiscoveryWorkScheduled ToCatalogDiscoveryWorkScheduled(CatalogDiscoveryWorkStoredEventRecordDto dto)
    {
        var data = dto.CatalogDiscoveryWorkScheduled
            ?? throw new InvalidOperationException("Missing catalog discovery work scheduled event data.");
        return new CatalogDiscoveryWorkScheduled(
            MusicCatalogId.From(data.MusicCatalogId),
            Enum.Parse<LookupPriorityBand>(data.Priority, ignoreCase: true),
            data.NextEligibleAtUtc,
            data.Reason,
            data.ScheduledAtUtc);
    }
}
