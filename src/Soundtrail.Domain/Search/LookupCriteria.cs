using Dunet;
using Soundtrail.Domain.Catalog;

namespace Soundtrail.Domain.Search;

[Union]
public partial record LookupCriteria
{
    public partial record Search(string SearchQuery, SearchTypes SearchTypes);
    
    public partial record IsrcLookup(string Isrc);
    
    public string NormalisedIdentifier =>
        Match<string>(
            search => "search:"+ StringNormalizationExtensions.Normalize(search.SearchQuery),
            isrcLookup => "isrc:" + StringNormalizationExtensions.Normalize(isrcLookup.Isrc));

    public static LookupCriteria Query(string query, SearchTypes searchTypes = SearchTypes.All) =>
        new Search(query ?? throw new ArgumentException("Value cannot be empty.", nameof(query)), searchTypes);
}

public enum SearchTypes
{
    Artist,
    Album,
    Track,
    All
}
