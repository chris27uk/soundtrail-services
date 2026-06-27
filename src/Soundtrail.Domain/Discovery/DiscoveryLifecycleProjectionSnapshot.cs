using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Model;

namespace Soundtrail.Domain.Discovery;

public sealed record DiscoveryLifecycleProjectionSnapshot(
    MusicSearchCriteria SearchCriteria,
    string Status,
    string Priority,
    bool WillBeLookedUp,
    int? EstimatedRetryAfterSeconds,
    DateTimeOffset? EarliestExpectedCompletionAt,
    string? Reason,
    DateTimeOffset UpdatedAt,
    int ProjectionVersion);
