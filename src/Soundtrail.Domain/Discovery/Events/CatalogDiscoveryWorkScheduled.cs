using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions.EventSourcing;

namespace Soundtrail.Domain.Discovery.Events;

public sealed record CatalogDiscoveryWorkScheduled(
    MusicCatalogId MusicCatalogId,
    LookupPriorityBand Priority,
    DateTimeOffset NextEligibleAt,
    string Reason,
    DateTimeOffset ScheduledAt) : IDomainEvent;
