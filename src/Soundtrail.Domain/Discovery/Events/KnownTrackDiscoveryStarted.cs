using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Catalog;

namespace Soundtrail.Domain.Discovery.Events;

public sealed record KnownTrackDiscoveryStarted(
    TrackId TrackId,
    LookupPriorityBand Priority,
    string Reason,
    DateTimeOffset StartedAt) : IDomainEvent;
