namespace Soundtrail.Domain.Search;

public sealed record LocalCatalogSearchResponse(
    IReadOnlyList<SearchCatalogResult> Results,
    SearchDiscovery? Discovery,
    bool IsComplete);
