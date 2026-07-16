using Soundtrail.Domain.Abstractions.EventSourcing;

namespace Soundtrail.Domain.Discovery.Events;

public sealed record WorkDeferred(
    EnrichmentTarget Target,
    string Reason,
    DateTimeOffset DeferredAt) : IDomainEvent;
