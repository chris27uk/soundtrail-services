using Soundtrail.Domain.Events;

namespace Soundtrail.Domain.Discovery;

public interface ICatalogSearchDiscoveryRepository
{
    Task<CatalogSearchDiscoveryEventStream> LoadAsync(
        CatalogSearchCriteria criteria,
        CancellationToken cancellationToken);

    Task<bool> AppendAsync(
        CatalogSearchCriteria criteria,
        int expectedVersion,
        IReadOnlyCollection<IDomainEvent> events,
        CancellationToken cancellationToken);
}
