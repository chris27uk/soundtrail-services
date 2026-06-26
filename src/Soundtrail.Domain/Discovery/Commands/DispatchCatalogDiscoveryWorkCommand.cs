using Soundtrail.Contracts.Common;

namespace Soundtrail.Domain.Discovery;

public sealed record DispatchCatalogDiscoveryWorkCommand(
    MusicCatalogId MusicCatalogId,
    DateTimeOffset RequestedAt,
    CorrelationId CorrelationId);
