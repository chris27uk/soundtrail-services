using Dunet;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Search;

namespace Soundtrail.Domain.Discovery;

[Union]
public partial record EnrichmentFilter
{
    public partial record SearchCriteria(LookupCriteria Value);

    public partial record CatalogItem(CatalogItemId Value);

    public string NormalisedIdentifier =>
        this switch
        {
            SearchCriteria(var searchCriteria) => $"search:{searchCriteria.NormalisedIdentifier}",
            CatalogItem(var itemId) => $"catalog-item:{itemId.NormalisedIdentifier}",
            _ => throw new InvalidOperationException($"Unsupported catalog item resource type '{GetType().Name}'.")
        };
}
