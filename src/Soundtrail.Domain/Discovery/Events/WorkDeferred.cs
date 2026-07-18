using Soundtrail.Domain.Common;
using Soundtrail.Domain.Abstractions.EventSourcing;

namespace Soundtrail.Domain.Discovery.Events;

public sealed record WorkDeferred(
    EnrichmentTarget Target,
    LookupPriorityBand Priority,
    DateTimeOffset NextEligibleAt,
    int? EstimatedRetryAfterSeconds,
    string Reason,
    DateTimeOffset DeferredAt) : IDomainEvent;
