using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions.EventSourcing;

namespace Soundtrail.Domain.Discovery;

public interface ICatalogDiscoveryWorkRepository
{
    Task<CatalogDiscoveryWorkEventStream> LoadAsync(
        MusicCatalogId musicCatalogId,
        CancellationToken cancellationToken);

    Task<bool> AppendAsync(
        MusicCatalogId musicCatalogId,
        int expectedVersion,
        IReadOnlyCollection<IDomainEvent> events,
        CancellationToken cancellationToken);
}
