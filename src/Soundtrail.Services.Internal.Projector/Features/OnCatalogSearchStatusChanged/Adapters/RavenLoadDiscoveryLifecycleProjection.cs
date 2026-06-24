using Raven.Client.Documents.Session;
using Soundtrail.Contracts;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchStatusChanged.Ports;
using Soundtrail.Domain.Discovery;

namespace Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchStatusChanged.Adapters;

public sealed class RavenLoadDiscoveryLifecycleProjection(
    IAsyncDocumentSession session,
    RavenDiscoveryLifecycleProjectionMapper mapper) : ILoadDiscoveryLifecycleProjectionPort
{
    public async Task<DiscoveryLifecycleProjection> LoadAsync(
        CatalogSearchCriteria criteria,
        CancellationToken cancellationToken)
    {
        var status = await session.LoadAsync<CatalogSearchStatusRecordDto>(
            CatalogSearchStatusRecordDto.GetDocumentId(criteria.Value),
            cancellationToken);
        var checkpoint = await session.LoadAsync<DiscoveryLifecycleProjectionCheckpointDocument>(
            DiscoveryLifecycleProjectionCheckpointDocument.GetDocumentId(criteria.Value),
            cancellationToken);
        return mapper.ToDomain(criteria, status, checkpoint);
    }
}
