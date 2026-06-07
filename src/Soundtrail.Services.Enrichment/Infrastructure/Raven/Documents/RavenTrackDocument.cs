namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Infrastructure.Raven.Documents;

public sealed class RavenTrackDocument
{
    public string Id { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Artist { get; set; } = string.Empty;

    public string SearchText { get; set; } = string.Empty;

    public string? Isrc { get; set; }

    public string? Mbid { get; set; }

    public string? AppleId { get; set; }

    public string? SpotifyId { get; set; }

    public int? DurationMs { get; set; }

    public RavenSongMetadataDocument? CanonicalMetadata { get; set; }

    public RavenProviderReferenceDocument? AppleReference { get; set; }

    public RavenProviderReferenceDocument? YouTubeMusicReference { get; set; }

    public bool IsPlayable { get; set; }

    public static string GetDocumentId(string stableId) => $"track-catalogue/{stableId}";

    public static string BuildSearchText(string title, string artist) =>
        $"{title} {artist}".Trim().ToLowerInvariant();
}

public sealed class RavenSongMetadataDocument
{
    public string Title { get; set; } = string.Empty;

    public string Artist { get; set; } = string.Empty;

    public string? Isrc { get; set; }

    public string? Mbid { get; set; }

    public int? DurationMs { get; set; }
}

public sealed class RavenProviderReferenceDocument
{
    public string Provider { get; set; } = string.Empty;

    public string Url { get; set; } = string.Empty;

    public string? ExternalId { get; set; }

    public string Confidence { get; set; } = string.Empty;

    public string SourceProvider { get; set; } = string.Empty;
}
