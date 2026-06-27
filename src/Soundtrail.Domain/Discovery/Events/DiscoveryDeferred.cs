using Soundtrail.Domain.Model;

namespace Soundtrail.Domain.Events;

public sealed record DiscoveryDeferred(
    MusicSearchCriteria SearchCriteria,
    bool WillBeLookedUp,
    int? EstimatedRetryAfterSeconds,
    DateTimeOffset? EarliestExpectedCompletionAt,
    string Reason,
    DateTimeOffset DeferredAt) : IDomainEvent;
