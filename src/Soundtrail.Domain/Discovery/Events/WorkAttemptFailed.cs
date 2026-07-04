using Soundtrail.Domain.Abstractions.EventSourcing;

namespace Soundtrail.Domain.Discovery.Events;

public sealed record WorkAttemptFailed(
    EnrichmentQuery Query,
    string Reason,
    DateTimeOffset FailedAt) : IDomainEvent;
