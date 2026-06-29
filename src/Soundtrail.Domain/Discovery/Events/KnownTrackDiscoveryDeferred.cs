using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Catalog;

namespace Soundtrail.Domain.Discovery.Events;

public sealed record KnownTrackDiscoveryDeferred(
    TrackId TrackId,
    int? EstimatedRetryAfterSeconds,
    DateTimeOffset? EarliestExpectedCompletionAt,
    string Reason,
    DateTimeOffset DeferredAt) : IDomainEvent;
