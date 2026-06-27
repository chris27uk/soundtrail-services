using Raven.Client.Documents.Session;
using Soundtrail.Contracts;
using Soundtrail.Domain.Search;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchStatusChanged.Adapters;
using Soundtrail.Tools.MusicBrainzImport.Features.ReplayDiscoveryLifecycleProjection.ProjectionReset;
using Soundtrail.Translators.Discovery;

namespace Soundtrail.Tools.MusicBrainzImport.Features.ReplayDiscoveryLifecycleProjection.Adapters;

public sealed class RavenResetDiscoveryLifecycleProjection(
    IAsyncDocumentSession session) : IResetDiscoveryLifecycleProjectionPort
{
    public async Task ResetAsync(
        MusicSearchCriteria searchCriteria,
        CancellationToken cancellationToken)
    {
        var persistentId = MusicSearchTermPersistentIdTranslator.ToPersistentId(searchCriteria);
        var changed = false;

        var status = await session.LoadAsync<CatalogSearchStatusRecordDto>(
            CatalogSearchStatusRecordDto.GetDocumentId(persistentId),
            cancellationToken);
        if (status is not null)
        {
            session.Delete(status);
            changed = true;
        }

        var checkpoint = await session.LoadAsync<DiscoveryLifecycleProjectionCheckpointDocument>(
            DiscoveryLifecycleProjectionCheckpointDocument.GetDocumentId(persistentId),
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
