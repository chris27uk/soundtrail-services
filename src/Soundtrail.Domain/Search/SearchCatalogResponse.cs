namespace Soundtrail.Domain.Search;

public sealed record SearchCatalogResponse(
    string Query,
    IReadOnlyList<SearchCatalogResult> Results,
    SearchDiscovery Discovery);
