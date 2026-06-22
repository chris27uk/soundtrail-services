namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Search.Resolution;

public sealed record MusicCatalogResolutionContext(
    string? NormalizedQuery,
    DateOnly? ReleaseDate)
{
    public static MusicCatalogResolutionContext Empty { get; } = new(null, null);
}
