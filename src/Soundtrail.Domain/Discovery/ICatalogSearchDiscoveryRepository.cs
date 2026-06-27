using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Discovery.Commands;
using Soundtrail.Domain.Search;

namespace Soundtrail.Domain.Discovery;

public interface ICatalogSearchDiscoveryRepository
{
    Task<CatalogSearchDiscoveryEventStream> LoadAsync(
        MusicSearchCriteria searchCriteria,
        CancellationToken cancellationToken);

    Task<CatalogSearchDiscoveryEventStream> LoadAsync(
        KnownCatalogItem knownItem,
        CancellationToken cancellationToken);

    Task<bool> AppendAsync(
        MusicSearchCriteria searchCriteria,
        int expectedVersion,
        IReadOnlyCollection<IDomainEvent> events,
        CancellationToken cancellationToken);

    Task<bool> AppendAsync(
        KnownCatalogItem knownItem,
        int expectedVersion,
        IReadOnlyCollection<IDomainEvent> events,
        CancellationToken cancellationToken);
}
