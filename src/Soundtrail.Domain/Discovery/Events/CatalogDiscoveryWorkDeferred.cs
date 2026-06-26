using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Events;

namespace Soundtrail.Domain.Discovery;

public sealed record CatalogDiscoveryWorkDeferred(
    MusicCatalogId MusicCatalogId,
    DateTimeOffset NextEligibleAt,
    string Reason,
    DateTimeOffset DeferredAt) : IDomainEvent;
