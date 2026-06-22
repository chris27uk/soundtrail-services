using Raven.Client.Documents.Session;
using Soundtrail.Contracts;
using Soundtrail.Domain.Discovery;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.ProjectDiscoveryLifecycle.Adapters;

namespace Soundtrail.Tools.MusicBrainzImport.Features.ReplayDiscoveryLifecycleProjection.Adapters;

public sealed class RavenResetDiscoveryLifecycleProjection(
    IAsyncDocumentSession session) : IResetDiscoveryLifecycleProjectionPort
{
    public async Task ResetAsync(
        CatalogSearchCriteria criteria,
        CancellationToken cancellationToken)
    {
        var changed = false;

        var status = await session.LoadAsync<CatalogSearchStatusRecordDto>(
            CatalogSearchStatusRecordDto.GetDocumentId(criteria.Value),
            cancellationToken);
        if (status is not null)
        {
            session.Delete(status);
            changed = true;
        }

        var checkpoint = await session.LoadAsync<DiscoveryLifecycleProjectionCheckpointDocument>(
            DiscoveryLifecycleProjectionCheckpointDocument.GetDocumentId(criteria.Value),
            cancellationToken);
        if (checkpoint is not null)
        {
            session.Delete(checkpoint);
            changed = true;
        }

        if (changed)
        {
            await session.SaveChangesAsync(cancellationToken);
            session.Advanced.Clear();
        }
    }
}
