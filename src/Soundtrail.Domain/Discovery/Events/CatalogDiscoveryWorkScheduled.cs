using Soundtrail.Contracts;
using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Events;

namespace Soundtrail.Domain.Discovery;

public sealed record CatalogDiscoveryWorkScheduled(
    MusicCatalogId MusicCatalogId,
    LookupPriorityBand Priority,
    DateTimeOffset NextEligibleAt,
    string Reason,
    DateTimeOffset ScheduledAt) : IDomainEvent;
