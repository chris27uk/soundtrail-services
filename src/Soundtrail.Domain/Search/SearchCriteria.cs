namespace Soundtrail.Domain.Search;

public record SearchCriteria(string Query, SearchTypes SearchTypes = SearchTypes.All)
{
    public string NormalisedIdentifier => "search:" + StringNormalizationExtensions.Normalize(this.Query);
}

public enum SearchTypes
{
    Artist,
    Album,
    Track,
    All
}
