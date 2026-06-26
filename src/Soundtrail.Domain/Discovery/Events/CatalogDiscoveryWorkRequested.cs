using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Events;

namespace Soundtrail.Domain.Discovery;

public sealed record CatalogDiscoveryWorkRequested(
    MusicCatalogId MusicCatalogId,
    int TrustLevel,
    int RiskScore,
    DateTimeOffset RequestedAt) : IDomainEvent;
