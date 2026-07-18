using Soundtrail.Domain.Common;
using Soundtrail.Domain.Abstractions.EventSourcing;

namespace Soundtrail.Domain.Discovery.Events;

public sealed record WorkIgnored(
    EnrichmentTarget Target,
    LookupPriorityBand Priority,
    DateTimeOffset? NextEligibleAt,
    int? EstimatedRetryAfterSeconds,
    DateTimeOffset? EarliestExpectedCompletionAt,
    string Reason,
    DateTimeOffset IgnoredAt) : IDomainEvent;
