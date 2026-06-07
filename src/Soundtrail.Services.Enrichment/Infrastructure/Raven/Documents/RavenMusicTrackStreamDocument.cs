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

public sealed class RavenMusicTrackFactDocument
{
    public string Type { get; set; } = string.Empty;

    public string SourceProvider { get; set; } = string.Empty;

    public DateTimeOffset ObservedAt { get; set; }

    public string? Title { get; set; }

    public string? Artist { get; set; }

    public int? DurationMs { get; set; }

    public string? Isrc { get; set; }

    public string? Mbid { get; set; }

    public string? Provider { get; set; }

    public string? ExternalId { get; set; }

    public string? Url { get; set; }

    public string? MusicCatalogId { get; set; }

    public string? Priority { get; set; }

    public string? CorrelationId { get; set; }

    public string? AlbumId { get; set; }

    public string? AlbumTitle { get; set; }

    public string? ArtistId { get; set; }

    public string? ArtistName { get; set; }
}
