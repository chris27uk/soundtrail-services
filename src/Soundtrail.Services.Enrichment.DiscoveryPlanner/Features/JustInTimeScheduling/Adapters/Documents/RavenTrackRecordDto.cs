namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.JustInTimeScheduling.Adapters.Documents;

public sealed class RavenTrackRecordDto
{
    public string Id { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Artist { get; set; } = string.Empty;

    public string? AlbumTitle { get; set; }

    public string SearchText { get; set; } = string.Empty;

    public string? Isrc { get; set; }

    public string? Mbid { get; set; }

    public string? AppleId { get; set; }

    public string? SpotifyId { get; set; }

    public int? DurationMs { get; set; }

    public RavenSongMetadataRecordDto? CanonicalMetadata { get; set; }

    public RavenProviderReferenceRecordDto? AppleReference { get; set; }

    public RavenProviderReferenceRecordDto? YouTubeMusicReference { get; set; }

    public bool IsPlayable { get; set; }

    public int ProjectionVersion { get; set; }

    public static string GetDocumentId(string stableId) => $"track-catalogue/{stableId}";

    public static string BuildSearchText(string title, string artist) =>
        $"{title} {artist}".Trim().ToLowerInvariant();
}
