using Soundtrail.Domain.Abstractions.EventSourcing;

namespace Soundtrail.Domain.Discovery.Events;

public sealed record WorkDeferred(
    EnrichmentQuery Query,
    string Reason,
    DateTimeOffset DeferredAt) : IDomainEvent;
