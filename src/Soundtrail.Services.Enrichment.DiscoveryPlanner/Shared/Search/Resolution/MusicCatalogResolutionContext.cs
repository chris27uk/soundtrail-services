namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Search.Resolution;

public sealed record MusicCatalogResolutionContext(DateOnly? ReleaseDate)
{
    public static MusicCatalogResolutionContext Empty { get; } = new((DateOnly?)null);
}
