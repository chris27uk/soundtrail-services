namespace Soundtrail.Services.Api.Infrastructure.Raven.Documents;

public sealed class CatalogTrackRecordDto
{
    public string Id { get; set; } = string.Empty;

    public string ArtistId { get; set; } = string.Empty;

    public string AlbumId { get; set; } = string.Empty;

    public string TrackId { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string NormalizedTitle { get; set; } = string.Empty;

    public string ArtistName { get; set; } = string.Empty;

    public string AlbumName { get; set; } = string.Empty;

    public string SearchText { get; set; } = string.Empty;

    public string? MusicBrainzRecordingId { get; set; }

    public string? Isrc { get; set; }

    public int? DurationMs { get; set; }

    public string[] AvailableProviders { get; set; } = [];

    public string[] TerminallyUnavailableProviders { get; set; } = [];

    public string? ArtworkUrl { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public static string GetDocumentId(string trackId) => $"catalog/tracks/{trackId}";
}
