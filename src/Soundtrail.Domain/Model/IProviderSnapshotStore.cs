namespace Soundtrail.Domain.Model;

public interface IProviderSnapshotStore
{
    Task SaveAsync(
        ProviderSnapshot snapshot,
        CancellationToken cancellationToken);
}
