using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions.EventSourcing;

namespace Soundtrail.Domain.Discovery.Events;

public sealed record CatalogDiscoveryWorkRequested(
    MusicCatalogId MusicCatalogId,
    int TrustLevel,
    int RiskScore,
    DateTimeOffset RequestedAt) : IDomainEvent;
