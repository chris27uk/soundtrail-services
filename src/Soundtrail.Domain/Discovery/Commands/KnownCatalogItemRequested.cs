using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Search;

namespace Soundtrail.Domain.Discovery.Commands;

public sealed record KnownCatalogItemRequested(
    KnownCatalogItem KnownItem,
    PlaybackProviderFilter Playback,
    int TrustLevel,
    int RiskScore,
    DateTimeOffset OccurredAt,
    CorrelationId CorrelationId);