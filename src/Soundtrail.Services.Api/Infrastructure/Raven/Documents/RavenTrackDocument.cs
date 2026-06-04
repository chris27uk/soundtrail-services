using Soundtrail.Services.Features.Search.TrackSearch;

namespace Soundtrail.Services.Api.Infrastructure.Raven.Documents;

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

    public static string GetDocumentId(string stableId) => $"track-catalogue/{stableId}";

    public static string BuildSearchText(string title, string artist)
    {
        var normalized = NormalizedSearchQuery.FromText($"{title} {artist}");
        return normalized.Value;
    }
}
