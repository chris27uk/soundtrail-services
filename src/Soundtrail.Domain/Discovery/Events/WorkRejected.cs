using Soundtrail.Domain.Common;
using Soundtrail.Domain.Abstractions.EventSourcing;

namespace Soundtrail.Domain.Discovery.Events;

public sealed record WorkRejected(
    EnrichmentTarget Target,
    LookupPriorityBand Priority,
    string Reason,
    DateTimeOffset RejectedAt) : IDomainEvent;
