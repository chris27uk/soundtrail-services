namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.EnrichmentResponse.Adapters.Documents;

public sealed class RavenMusicTrackStreamRecordDto
{
    public string Id { get; set; } = string.Empty;

    public string MusicCatalogId { get; set; } = string.Empty;

    public int Version { get; set; }

    public List<string> AppliedCommandIds { get; set; } = [];

    public List<RavenMusicTrackEventRecordDto> Events { get; set; } = [];

    public static string GetDocumentId(string stableId) => $"music-track-streams/{stableId}";
}
