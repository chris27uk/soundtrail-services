namespace Soundtrail.Services.Internal.Projector.Features.OnCatalogCandidateIdentified.Adapters.Documents;

public sealed class PotentialCatalogLookupWorkRecordDto
{
    public string Id { get; set; } = string.Empty;

    public string MusicCatalogId { get; set; } = string.Empty;

    public int RequestCount { get; set; }

    public int HighestTrustLevelSeen { get; set; }

    public int RiskScore { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTimeOffset? NextEligibleAt { get; set; }

    public List<string> AppliedSearchStartEventIds { get; set; } = [];

    public static string GetDocumentId(string musicCatalogId) =>
        $"potential-catalog-lookup-work/{Uri.EscapeDataString(musicCatalogId)}";
}
