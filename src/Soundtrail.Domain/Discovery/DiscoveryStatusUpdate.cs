using Soundtrail.Contracts.Common;

namespace Soundtrail.Domain.Discovery;

public sealed record DiscoveryStatusUpdate(
    DiscoveryQueryKey QueryKey,
    DiscoveryLifecycleStatus Status,
    LookupPriorityBand? Priority,
    bool WillBeLookedUp,
    int? EstimatedRetryAfterSeconds,
    DateTimeOffset? EarliestExpectedCompletionAt,
    string? Reason,
    DateTimeOffset UpdatedAt);
