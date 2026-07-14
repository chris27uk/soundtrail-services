using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions.EventSourcing;

namespace Soundtrail.Domain.Discovery.Events;

public sealed record WorkCompleted(
    EnrichmentFilter Filter,
    LookupPriorityBandDto Priority,
    string Reason,
    DateTimeOffset CompletedAt) : IDomainEvent;
