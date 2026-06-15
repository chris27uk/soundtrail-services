namespace Soundtrail.Domain.Discovery;

public interface IUpsertCatalogSearchStatusPort
{
    Task UpsertAsync(
        CatalogSearchStatusUpdate update,
        CancellationToken cancellationToken);
}
