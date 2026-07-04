using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Search;

namespace Soundtrail.Domain.Discovery.Events;

public sealed record WorkRejected(
    EnrichmentQuery Query,
    string Reason,
    DateTimeOffset RejectedAt) : IDomainEvent;
