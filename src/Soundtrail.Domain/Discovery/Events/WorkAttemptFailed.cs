using Soundtrail.Domain.Abstractions.EventSourcing;

namespace Soundtrail.Domain.Discovery.Events;

public sealed record WorkAttemptFailed(
    EnrichmentTarget Target,
    string Reason,
    DateTimeOffset FailedAt) : IDomainEvent;
