namespace Soundtrail.Contracts.Persistence;

public sealed class RavenTrackRecordDto
{
    public string Id { get; set; } = string.Empty;

    public string? ArtistId { get; set; }

    public string? AlbumId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Artist { get; set; } = string.Empty;

    public string NormalizedArtist { get; set; } = string.Empty;

    public string? AlbumTitle { get; set; }

    public string NormalizedAlbumTitle { get; set; } = string.Empty;

    public string SearchText { get; set; } = string.Empty;

    public string? Isrc { get; set; }

    public string NormalizedIsrc { get; set; } = string.Empty;

    public string? Mbid { get; set; }

    public string NormalizedMbid { get; set; } = string.Empty;

    public string? AppleId { get; set; }

    public string? SpotifyId { get; set; }

    public int? DurationMs { get; set; }

    public DateOnly? ReleaseDate { get; set; }

    public string? ArtworkUrl { get; set; }

    public RavenSongMetadataRecordDto? CanonicalMetadata { get; set; }

    public RavenProviderReferenceRecordDto? AppleReference { get; set; }

    public RavenProviderReferenceRecordDto? YouTubeMusicReference { get; set; }

    public bool IsPlayable { get; set; }

    public int ProjectionVersion { get; set; }

    public static string GetDocumentId(string stableId) => $"track-catalogue/{stableId}";

    public static string BuildSearchText(string title, string artist) =>
        NormalizeFreeText($"{title} {artist}".Trim());

    private static string NormalizeFreeText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var sanitized = new string(
            value
                .Trim()
                .Select(character => char.IsLetterOrDigit(character) || char.IsWhiteSpace(character)
                    ? char.ToLowerInvariant(character)
                    : ' ')
                .ToArray());

        return string.Join(
            ' ',
            sanitized.Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }
}
