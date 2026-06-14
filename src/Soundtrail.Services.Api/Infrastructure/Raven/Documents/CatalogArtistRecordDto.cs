namespace Soundtrail.Services.Api.Infrastructure.Raven.Documents;

internal sealed class CatalogArtistRecordDto
{
    public string Id { get; set; } = string.Empty;

    public string ArtistId { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string NormalizedName { get; set; } = string.Empty;

    public string? MusicBrainzArtistId { get; set; }

    public string[] AvailableProviders { get; set; } = [];

    public string[] TerminallyUnavailableProviders { get; set; } = [];

    public string? ArtworkUrl { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public static string GetDocumentId(string artistId) => $"catalog/artists/{artistId}";
}
