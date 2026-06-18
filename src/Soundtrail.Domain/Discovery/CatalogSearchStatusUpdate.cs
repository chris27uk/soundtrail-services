using Soundtrail.Contracts.Common;

namespace Soundtrail.Domain.Discovery;

public sealed record CatalogSearchStatusUpdate(
    CatalogSearchCriteria Criteria,
    CatalogSearchLifecycleStatus Status,
    LookupPriorityBand? Priority,
    bool WillBeLookedUp,
    int? EstimatedRetryAfterSeconds,
    DateTimeOffset? EarliestExpectedCompletionAt,
    string? Reason,
    DateTimeOffset UpdatedAt);
