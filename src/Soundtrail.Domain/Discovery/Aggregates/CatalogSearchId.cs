using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Search;

namespace Soundtrail.Domain.Discovery.Aggregates;

public readonly record struct CatalogSearchId(string StableValue) : IValueType
{
    public static CatalogSearchId From(SearchCriteria searchCriteria) => new(searchCriteria.NormalisedIdentifier);

    public override string ToString() => StableValue;
}
