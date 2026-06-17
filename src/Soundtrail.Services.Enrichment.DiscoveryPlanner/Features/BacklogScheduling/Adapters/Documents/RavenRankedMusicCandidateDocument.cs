namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.BacklogScheduling.Adapters.Documents;

internal sealed class RavenRankedMusicCandidateDocument
{
    public string Id { get; set; } = string.Empty;

    public string MusicCatalogId { get; set; } = string.Empty;

    public int RequestCount { get; set; }

    public int HighestTrustLevelSeen { get; set; }

    public int RiskScore { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTimeOffset? NextEligibleAt { get; set; }

    public static string GetDocumentId(string musicCatalogId) =>
        $"ranked-music-candidates/{Uri.EscapeDataString(musicCatalogId)}";
}
