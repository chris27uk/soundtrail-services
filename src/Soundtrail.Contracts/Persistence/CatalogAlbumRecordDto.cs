namespace Soundtrail.Services.Api.Infrastructure.Raven.Documents;

public sealed class CatalogAlbumRecordDto
{
    public string Id { get; set; } = string.Empty;

    public string ArtistId { get; set; } = string.Empty;

    public string AlbumId { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string NormalizedName { get; set; } = string.Empty;

    public string ArtistName { get; set; } = string.Empty;

    public string SearchText { get; set; } = string.Empty;

    public string? MusicBrainzReleaseId { get; set; }

    public string[] AvailableProviders { get; set; } = [];

    public string[] TerminallyUnavailableProviders { get; set; } = [];

    public string? ArtworkUrl { get; set; }

    public DateOnly? ReleaseDate { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public static string GetDocumentId(string albumId) => $"catalog/albums/{albumId}";
}
