using Soundtrail.Domain.Abstractions.EventSourcing;

namespace Soundtrail.Domain.Discovery.Events;

public sealed record WorkAttemptFailed(
    EnrichmentFilter Filter,
    string Reason,
    DateTimeOffset FailedAt) : IDomainEvent;
