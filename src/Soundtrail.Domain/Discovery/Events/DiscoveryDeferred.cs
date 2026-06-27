using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Search;

namespace Soundtrail.Domain.Discovery.Events;

public sealed record DiscoveryDeferred(
    MusicSearchCriteria SearchCriteria,
    bool WillBeLookedUp,
    int? EstimatedRetryAfterSeconds,
    DateTimeOffset? EarliestExpectedCompletionAt,
    string Reason,
    DateTimeOffset DeferredAt) : IDomainEvent;
