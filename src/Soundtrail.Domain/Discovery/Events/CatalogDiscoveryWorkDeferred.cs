using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions.EventSourcing;

namespace Soundtrail.Domain.Discovery.Events;

public sealed record CatalogDiscoveryWorkDeferred(
    MusicCatalogId MusicCatalogId,
    DateTimeOffset NextEligibleAt,
    string Reason,
    DateTimeOffset DeferredAt) : IDomainEvent;
