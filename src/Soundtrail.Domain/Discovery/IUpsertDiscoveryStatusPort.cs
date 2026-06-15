namespace Soundtrail.Domain.Discovery;

public interface IUpsertDiscoveryStatusPort
{
    Task UpsertAsync(
        DiscoveryStatusUpdate update,
        CancellationToken cancellationToken);
}
