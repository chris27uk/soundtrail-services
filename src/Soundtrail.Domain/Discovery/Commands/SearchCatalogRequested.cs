using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Search;

namespace Soundtrail.Domain.Discovery.Commands;

public sealed record SearchCatalogRequested(
    MusicSearchCriteria SearchCriteria,
    PlaybackProviderFilter Playback,
    int TrustLevel,
    int RiskScore,
    DateTimeOffset OccurredAt,
    CorrelationId CorrelationId);
