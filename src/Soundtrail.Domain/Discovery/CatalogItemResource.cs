using Dunet;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Search;

namespace Soundtrail.Domain.Discovery;

[Union]
public partial record CatalogItemResource
{
    public partial record SearchCriteria(MusicSearchCriteria Value);

    public partial record CatalogItem(CatalogItemId Value);

    public string StableValue =>
        this switch
        {
            SearchCriteria(var searchCriteria) => $"search:{DiscoveryQueryKey.StableValueFor(searchCriteria)}",
            CatalogItem(var itemId) => $"catalog-item:{itemId.EntityKind}:{itemId.StableValue}",
            _ => throw new InvalidOperationException($"Unsupported catalog item resource type '{GetType().Name}'.")
        };

    public static CatalogItemResource ForSearch(MusicSearchCriteria searchCriteria) => new SearchCriteria(searchCriteria);

    public static CatalogItemResource ForCatalogItem(CatalogItemId itemId) => new CatalogItem(itemId);

    public MusicSearchCriteria RequireSearchCriteria() =>
        this switch
        {
            SearchCriteria(var searchCriteria) => searchCriteria,
            _ => throw new InvalidOperationException("Catalog item resource must be a search criteria.")
        };
}
