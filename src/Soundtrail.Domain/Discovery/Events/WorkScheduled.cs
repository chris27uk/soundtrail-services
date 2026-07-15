using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Common;

namespace Soundtrail.Domain.Discovery.Events;

public sealed record WorkScheduled(
    EnrichmentFilter Filter,
    LookupPriorityBand Priority,
    string Reason,
    DateTimeOffset ScheduledAt) : IDomainEvent;
