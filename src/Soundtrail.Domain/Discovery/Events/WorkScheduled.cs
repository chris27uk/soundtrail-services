using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Common;

namespace Soundtrail.Domain.Discovery.Events;

public sealed record WorkScheduled(
    EnrichmentTarget Target,
    LookupPriorityBand Priority,
    string Reason,
    DateTimeOffset ScheduledAt) : IDomainEvent;
