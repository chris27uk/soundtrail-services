using Raven.Client.Documents.Session;
using Soundtrail.Contracts;
using Soundtrail.Domain.Discovery;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.ProjectDiscoveryLifecycle.Adapters;

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
