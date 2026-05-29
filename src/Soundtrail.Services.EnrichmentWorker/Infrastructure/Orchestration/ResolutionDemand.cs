using Soundtrail.Services.EnrichmentWorker.Jobs;
using Soundtrail.Services.Features.Search.Models;
using Soundtrail.Services.Features.Tracks;

namespace Soundtrail.Services.EnrichmentWorker.Models;

public sealed record ResolutionDemand(
    QueryId QueryId,
    NormalizedSearchQuery NormalizedQuery,
    int DemandCount,
    int DistinctInstallCount,
    int DistinctIpHashCount,
    int HighestTrustLevelSeen,
    int RiskScore,
    TrackTitle? BestKnownTitle,
    ArtistName? BestKnownArtist,
    Isrc? BestKnownIsrc,
    Mbid? BestKnownMbid,
    ResolutionDemandStatus Status,
    DateTimeOffset FirstSeenAt,
    DateTimeOffset LastSeenAt,
    DateTimeOffset? NextEligibleAt,
    IReadOnlyCollection<EnrichmentStage>? AttemptedStages = null,
    int PreviousFailureCount = 0)
{
    public IReadOnlyCollection<EnrichmentStage> AttemptedStages { get; init; } =
        AttemptedStages ?? Array.Empty<EnrichmentStage>();

    public bool HasStrongMetadata => BestKnownArtist is not null && BestKnownTitle is not null;

    public bool IsSuspicious => RiskScore >= 100;
}

public enum ResolutionDemandStatus
{
    Unresolved = 0,
    PartiallyResolved = 1,
    Resolved = 2,
    Rejected = 3
}
