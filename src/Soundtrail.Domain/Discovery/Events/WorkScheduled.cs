using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions.EventSourcing;

namespace Soundtrail.Domain.Discovery.Events;

public sealed record WorkScheduled(
    EnrichmentFilter Filter,
    LookupPriorityBand Priority,
    string Reason,
    DateTimeOffset ScheduledAt) : IDomainEvent;
