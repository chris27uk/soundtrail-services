using Soundtrail.Domain.Search;
using Soundtrail.Services.Api.Features.Catalog.Search.Contract;

namespace Soundtrail.Services.Api.Features.Catalog.Search.Adapters;

public interface ISearchPort
{
    Task<SearchResponse?> SearchAsync(SearchCriteria searchCriteria, CancellationToken cancellationToken);
}
