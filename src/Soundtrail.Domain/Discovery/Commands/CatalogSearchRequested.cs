using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Model;
using Soundtrail.Domain.Search;

namespace Soundtrail.Domain.Commands;

public sealed record CatalogSearchRequested(
    MusicSeekOrSearchCriteria Criteria,
    PlaybackProviderFilter Playback,
    int TrustLevel,
    int RiskScore,
    DateTimeOffset OccurredAt,
    CorrelationId CorrelationId);
