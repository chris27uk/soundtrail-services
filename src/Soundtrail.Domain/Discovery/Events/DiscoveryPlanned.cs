using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Discovery;

namespace Soundtrail.Domain.Events;

public sealed record DiscoveryPlanned(
    CatalogSearchCriteria Criteria,
    LookupPriorityBand Priority,
    bool WillBeLookedUp,
    int? EstimatedRetryAfterSeconds,
    DateTimeOffset? EarliestExpectedCompletionAt,
    string Reason,
    DateTimeOffset PlannedAt) : IDomainEvent;
