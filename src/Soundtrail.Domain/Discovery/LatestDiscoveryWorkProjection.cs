using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Discovery.Events;

namespace Soundtrail.Domain.Discovery;

public sealed class LatestDiscoveryWorkProjection
{
    private readonly EventHandlers<LatestDiscoveryWorkProjection> eventHandlers;

    public LatestDiscoveryWorkProjection(MusicCatalogId musicCatalogId)
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

    public static LatestDiscoveryWorkProjection Load(CatalogDiscoveryWorkSummarySnapshot snapshot) =>
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
        this.eventHandlers.Handle(@event);
        LastAppliedVersion = version;
    }

    private EventHandlers<LatestDiscoveryWorkProjection> CreateHandlers()
    {
        var handlers = new EventHandlers<LatestDiscoveryWorkProjection>();
        handlers.Register<CatalogDiscoveryWorkRequested>(On);
        handlers.Register<CatalogDiscoveryWorkDeferred>(On);
        handlers.Register<CatalogDiscoveryWorkIgnored>(On);
        handlers.Register<CatalogDiscoveryWorkScheduled>(On);
        return handlers;
    }

    private void On(CatalogDiscoveryWorkRequested @event)
    {
        RequestCount += 1;
        HighestTrustLevelSeen = Math.Max(HighestTrustLevelSeen, @event.TrustLevel);
        RiskScore = Math.Max(RiskScore, @event.RiskScore);
        Status = @event.RiskScore >= 90
            ? CatalogDiscoveryWorkStatus.Ignored
            : CatalogDiscoveryWorkStatus.Pending;
        NextEligibleAt = null;
        Priority = null;
        Reason = null;
        UpdatedAt = @event.RequestedAt;
    }

    private void On(CatalogDiscoveryWorkDeferred @event)
    {
        Status = CatalogDiscoveryWorkStatus.Pending;
        NextEligibleAt = @event.NextEligibleAt;
        Priority = null;
        Reason = @event.Reason;
        UpdatedAt = @event.DeferredAt;
    }

    private void On(CatalogDiscoveryWorkIgnored @event)
    {
        Status = CatalogDiscoveryWorkStatus.Ignored;
        NextEligibleAt = @event.NextEligibleAt;
        Priority = null;
        Reason = @event.Reason;
        UpdatedAt = @event.IgnoredAt;
    }

    private void On(CatalogDiscoveryWorkScheduled @event)
    {
        Status = CatalogDiscoveryWorkStatus.Pending;
        NextEligibleAt = @event.NextEligibleAt;
        Priority = @event.Priority;
        Reason = @event.Reason;
        UpdatedAt = @event.ScheduledAt;
    }
}
