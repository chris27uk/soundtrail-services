namespace Soundtrail.Services.Enrichment.Orchestrator.Infrastructure.Raven.CatalogDiscoveryWork;

public sealed class CatalogDiscoveryWorkSummaryRecordDto
{
    public string Id { get; set; } = string.Empty;

    public string MusicCatalogId { get; set; } = string.Empty;

    public int RequestCount { get; set; }

    public int HighestTrustLevelSeen { get; set; }

    public int RiskScore { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTimeOffset? NextEligibleAt { get; set; }

    public string? Priority { get; set; }

    public string? Reason { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; }

    public int LastAppliedVersion { get; set; }

    public static string GetDocumentId(string musicCatalogId) => $"catalog-discovery-work-summary/{musicCatalogId}";
}
