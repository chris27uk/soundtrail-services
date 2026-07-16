using Dunet;
using Soundtrail.Domain.Catalog;

namespace Soundtrail.Domain.Discovery;

[Union]
public partial record EnrichmentTarget
{
    public partial record Unknown(Search.SearchCriteria Value);

    public partial record Existing(CatalogItemId Value);

    public string NormalisedIdentifier =>
        this switch
        {
            Unknown(var searchCriteria) => searchCriteria.NormalisedIdentifier,
            Existing(var itemId) => $"catalog-item:{itemId.NormalisedIdentifier}",
            _ => throw new InvalidOperationException($"Unsupported catalog item resource type '{GetType().Name}'.")
        };
}
