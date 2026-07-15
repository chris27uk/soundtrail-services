using Soundtrail.Domain.Abstractions.EventSourcing;

namespace Soundtrail.Domain.Discovery.Events;

public sealed record WorkDeferred(
    EnrichmentFilter Filter,
    string Reason,
    DateTimeOffset DeferredAt) : IDomainEvent;
