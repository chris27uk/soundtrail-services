using Soundtrail.Services.Api.Features.Search.Contract;

namespace Soundtrail.Domain.Search;

public record SearchCriteria(string Query, SearchFilter SearchTypes = SearchFilter.All)
{
    public string NormalisedIdentifier => "search:" + StringNormalizationExtensions.Normalize(this.Query);
}
