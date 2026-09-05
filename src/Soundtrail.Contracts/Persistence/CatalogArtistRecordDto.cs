namespace Soundtrail.Contracts.Persistence;

public sealed class CatalogArtistRecordDto
{
    public string Id { get; set; } = string.Empty;

    public string ArtistId { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string NormalizedName { get; set; } = string.Empty;

    public string SearchText { get; set; } = string.Empty;

    public string? MusicBrainzArtistId { get; set; }

    public string[] AvailableProviders { get; set; } = [];

    public string[] TerminallyUnavailableProviders { get; set; } = [];

    public string? ArtworkUrl { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>
    /// Artist-catalog stream version last fully projected into browse/search docs.
    /// Used to re-project after later dump phases append albums/tracks for the same observation.
    /// </summary>
    public int? ProjectedStreamVersion { get; set; }

    public static string GetDocumentId(string artistId) => $"catalog/artists/{artistId}";
}
