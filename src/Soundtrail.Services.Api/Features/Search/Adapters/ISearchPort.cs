using Soundtrail.Domain.Search;
using Soundtrail.Services.Api.Features.Search.Contract;

namespace Soundtrail.Services.Api.Features.Search.Adapters;

public interface ISearchPort
{
    Task<SearchResponse?> SearchAsync(SearchCriteria searchCriteria, CancellationToken cancellationToken);
}
