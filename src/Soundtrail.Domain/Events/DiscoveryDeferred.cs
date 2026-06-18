using Soundtrail.Domain.Discovery;

namespace Soundtrail.Domain.Events;

public sealed record DiscoveryDeferred(
    CatalogSearchCriteria Criteria,
    bool WillBeLookedUp,
    int? EstimatedRetryAfterSeconds,
    DateTimeOffset? EarliestExpectedCompletionAt,
    string Reason,
    DateTimeOffset DeferredAt) : IDomainEvent;
