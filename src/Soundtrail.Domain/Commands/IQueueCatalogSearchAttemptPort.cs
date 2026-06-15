namespace Soundtrail.Domain.Commands;

public interface IQueueCatalogSearchAttemptPort
{
    Task EnqueueAsync(CatalogSearchAttempt request, CancellationToken cancellationToken);
}
