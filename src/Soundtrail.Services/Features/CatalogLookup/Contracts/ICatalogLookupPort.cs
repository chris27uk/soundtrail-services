namespace Soundtrail.Services.Features.CatalogLookup.Contracts;

public interface ICatalogLookupPort
{
    Task<bool> IsReadyAsync(CancellationToken cancellationToken);
}
