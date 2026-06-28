namespace Soundtrail.Services.Internal.Projector.Features.OnCatalogCandidateIdentified.Support;

public sealed class PotentialCatalogLookupWorkState
{
    public required string MusicCatalogId { get; init; }
    public required string Status { get; set; }
    public int RequestCount { get; set; }
    public int HighestTrustLevelSeen { get; set; }
    public int RiskScore { get; set; }
    public List<string> AppliedSearchStartEventIds { get; init; } = [];
}
