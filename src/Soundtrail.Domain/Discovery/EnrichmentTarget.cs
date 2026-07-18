using Dunet;
using Soundtrail.Domain.Search;

namespace Soundtrail.Domain.Discovery;

[Union]
public partial record EnrichmentTarget
{
    public partial record SearchForUnknownCatalogItem(SearchCriteria Criteria);
    
    public partial record KnownCatalogItemOperation(CatalogItemOperation Operation);
    
    public string NormalisedIdentifier =>
        this switch
        {
            SearchForUnknownCatalogItem(var searchCriteria) => searchCriteria.NormalisedIdentifier,
            KnownCatalogItemOperation(var operation) => operation.StableIdentifier(),
            _ => throw new InvalidOperationException($"Unsupported catalog item resource type '{GetType().Name}'.")
        };
}
