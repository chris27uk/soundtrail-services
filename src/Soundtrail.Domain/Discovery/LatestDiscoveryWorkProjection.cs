using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Discovery.Events;

namespace Soundtrail.Domain.Discovery;

public sealed class CatalogDiscoveryWorkSummaryProjection
{
    private readonly EventHandlers<CatalogDiscoveryWorkSummaryProjection> eventHandlers;

    public CatalogDiscoveryWorkSummaryProjection(MusicCatalogId musicCatalogId)
    {
        MusicCatalogId = musicCatalogId;
        eventHandlers = CreateHandlers();
    }

    public MusicCatalogId MusicCatalogId { get; }

    public int RequestCount { get; private set; }

    public int HighestTrustLevelSeen { get; private set; }

    public int RiskScore { get; private set; }

    public CatalogDiscoveryWorkStatus Status { get; private set; }

    public DateTimeOffset? NextEligibleAt { get; private set; }

    public LookupPriorityBand? Priority { get; private set; }

    public string? Reason { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public int LastAppliedVersion { get; private set; }

    public static CatalogDiscoveryWorkSummaryProjection Load(CatalogDiscoveryWorkSummarySnapshot snapshot) =>
        new(snapshot.MusicCatalogId)
        {
            RequestCount = snapshot.RequestCount,
            HighestTrustLevelSeen = snapshot.HighestTrustLevelSeen,
            RiskScore = snapshot.RiskScore,
            Status = snapshot.Status,
            NextEligibleAt = snapshot.NextEligibleAt,
            Priority = snapshot.Priority,
            Reason = snapshot.Reason,
            UpdatedAt = snapshot.UpdatedAt,
            LastAppliedVersion = snapshot.LastAppliedVersion
        };

    public CatalogDiscoveryWorkSummary ToSummary() =>
        new(
            MusicCatalogId,
            RequestCount,
            HighestTrustLevelSeen,
            RiskScore,
            Status,
            NextEligibleAt,
            Priority,
            Reason);

    public CatalogDiscoveryWorkSummarySnapshot ToSnapshot() =>
        new(
            MusicCatalogId,
            RequestCount,
            HighestTrustLevelSeen,
            RiskScore,
            Status,
            NextEligibleAt,
            Priority,
            Reason,
            UpdatedAt,
            LastAppliedVersion);

    public void Apply(IDomainEvent @event, int version)
    {
        if (version <= LastAppliedVersion)
        {
            return;
        }

        eventHandlers.Handle(@event);
        LastAppliedVersion = version;
    }

    private EventHandlers<CatalogDiscoveryWorkSummaryProjection> CreateHandlers()
    {
        var handlers = new EventHandlers<CatalogDiscoveryWorkSummaryProjection>();

        handlers.Register<CatalogDiscoveryWorkRequested>(requested =>
        {
            RequestCount += 1;
            HighestTrustLevelSeen = Math.Max(HighestTrustLevelSeen, requested.TrustLevel);
            RiskScore = Math.Max(RiskScore, requested.RiskScore);
            Status = requested.RiskScore >= 90
                ? CatalogDiscoveryWorkStatus.Ignored
                : CatalogDiscoveryWorkStatus.Pending;
            NextEligibleAt = null;
            Priority = null;
            Reason = null;
            UpdatedAt = requested.RequestedAt;
        });

        handlers.Register<CatalogDiscoveryWorkDeferred>(deferred =>
        {
            Status = CatalogDiscoveryWorkStatus.Pending;
            NextEligibleAt = deferred.NextEligibleAt;
            Priority = null;
            Reason = deferred.Reason;
            UpdatedAt = deferred.DeferredAt;
        });

        handlers.Register<CatalogDiscoveryWorkIgnored>(ignored =>
        {
            Status = CatalogDiscoveryWorkStatus.Ignored;
            NextEligibleAt = ignored.NextEligibleAt;
            Priority = null;
            Reason = ignored.Reason;
            UpdatedAt = ignored.IgnoredAt;
        });

        handlers.Register<CatalogDiscoveryWorkScheduled>(scheduled =>
        {
            Status = CatalogDiscoveryWorkStatus.Pending;
            NextEligibleAt = scheduled.NextEligibleAt;
            Priority = scheduled.Priority;
            Reason = scheduled.Reason;
            UpdatedAt = scheduled.ScheduledAt;
        });

        return handlers;
    }
}
