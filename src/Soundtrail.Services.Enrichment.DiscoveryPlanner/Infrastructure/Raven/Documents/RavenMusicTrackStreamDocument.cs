namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Infrastructure.Raven.Documents;

public sealed class RavenMusicTrackStreamDocument
{
    public string Id { get; set; } = string.Empty;

    public string MusicCatalogId { get; set; } = string.Empty;

    public int Version { get; set; }

    public List<string> AppliedCommandIds { get; set; } = [];

    public List<RavenMusicTrackFactDocument> Facts { get; set; } = [];

    public static string GetDocumentId(string stableId) => $"music-track-streams/{stableId}";
}