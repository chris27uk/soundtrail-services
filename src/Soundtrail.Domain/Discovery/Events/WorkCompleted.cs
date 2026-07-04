using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions.EventSourcing;

namespace Soundtrail.Domain.Discovery.Events;

public sealed record WorkCompleted(
    EnrichmentQuery Query,
    LookupPriorityBand Priority,
    string Reason,
    DateTimeOffset CompletedAt) : IDomainEvent;
