using Soundtrail.Domain.Search;

namespace Soundtrail.Services.Api.Features.SearchCatalog.Ports;

public interface ICatalogSearchPort
{
    Task<CandidateSearchResponse> SearchAsync(SearchCatalogCommand command, CancellationToken cancellationToken);
}
