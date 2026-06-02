namespace Soundtrail.Services.Api.Infrastructure.Raven.Documents;

internal sealed class RavenSearchResultDocument
{
    public string Title { get; set; } = string.Empty;

    public string Artist { get; set; } = string.Empty;

    public string? Isrc { get; set; }

    public string? Mbid { get; set; }

    public string? AppleId { get; set; }

    public string? SpotifyId { get; set; }

    public double Confidence { get; set; }
}
