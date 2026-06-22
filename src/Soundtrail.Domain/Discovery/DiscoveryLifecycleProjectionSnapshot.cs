using Soundtrail.Contracts.Common;

namespace Soundtrail.Domain.Discovery;

public sealed record DiscoveryLifecycleProjectionSnapshot(
    CatalogSearchCriteria Criteria,
    string Status,
    string Priority,
    bool WillBeLookedUp,
    int? EstimatedRetryAfterSeconds,
    DateTimeOffset? EarliestExpectedCompletionAt,
    string? Reason,
    DateTimeOffset UpdatedAt,
    int ProjectionVersion);
