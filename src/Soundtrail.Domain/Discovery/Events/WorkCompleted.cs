using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Common;

namespace Soundtrail.Domain.Discovery.Events;

public sealed record WorkCompleted(
    EnrichmentFilter Filter,
    LookupPriorityBand Priority,
    string Reason,
    DateTimeOffset CompletedAt) : IDomainEvent;
