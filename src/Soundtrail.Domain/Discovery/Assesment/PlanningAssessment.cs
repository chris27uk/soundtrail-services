using Soundtrail.Domain.Common;

namespace Soundtrail.Domain.Discovery.Assesment;

public sealed record PlanningAssessment(
    EnrichmentTarget Target,
    LookupPriorityBand Priority,
    DateTimeOffset RequestedAt,
    int? TrustLevel,
    int? RiskScore,
    DiscoveryPlanningProjection Projection)
{
    public DateTimeOffset DeferredUntil { get; init; } = RequestedAt;

    public DateTimeOffset ExpectedCompletionAt { get; init; } = RequestedAt;

    public bool HighPriorityCapacityIsProtected { get; init; }

    public bool PlannerCapacityIsFull { get; init; }
}
