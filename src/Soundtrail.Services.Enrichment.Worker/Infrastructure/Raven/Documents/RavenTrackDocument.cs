namespace Soundtrail.Services.Enrichment.Worker.Infrastructure.Raven.Documents;

internal sealed class RavenTrackDocument
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
}
