using Soundtrail.Domain.Search;

namespace Soundtrail.Services.Api.Features.SearchCatalog.Ports;

public interface ICatalogSearchPort
{
    Task<LocalCatalogSearchResponse> SearchAsync(SearchCatalogCommand command, CancellationToken cancellationToken);
}
