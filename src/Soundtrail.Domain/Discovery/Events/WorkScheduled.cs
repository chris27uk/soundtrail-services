using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions.EventSourcing;

namespace Soundtrail.Domain.Discovery.Events;

public sealed record WorkScheduled(
    EnrichmentQuery Query,
    LookupPriorityBand Priority,
    string Reason,
    DateTimeOffset ScheduledAt) : IDomainEvent;
