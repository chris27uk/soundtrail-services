namespace Soundtrail.Domain.Search;

public interface ICatalogSearchPort
{
    Task<LocalCatalogSearchResponse> SearchAsync(SearchCatalogCommand command, CancellationToken cancellationToken);
}
