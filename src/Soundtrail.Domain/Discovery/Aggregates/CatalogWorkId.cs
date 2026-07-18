using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Search;

namespace Soundtrail.Domain.Discovery.Aggregates;

public readonly record struct CatalogWorkId(string StableValue) : IValueType
{
    public static CatalogWorkId From(SearchCriteria searchCriteria) => new(searchCriteria.NormalisedIdentifier);

    public static CatalogWorkId From(CatalogItemOperation operation) => new(operation.StableIdentifier());

    public static CatalogWorkId From(EnrichmentTarget target) => new(target.NormalisedIdentifier);

    public override string ToString() => StableValue;
}
