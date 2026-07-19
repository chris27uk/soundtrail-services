namespace Soundtrail.Domain.Search;

public record SearchCriteria(string Query, SearchType SearchTypes = SearchType.All)
{
    public string NormalisedIdentifier => "search:" + StringNormalizationExtensions.Normalize(this.Query);
}
