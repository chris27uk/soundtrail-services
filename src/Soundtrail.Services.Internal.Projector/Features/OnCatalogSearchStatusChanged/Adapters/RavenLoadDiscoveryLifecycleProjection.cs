using Raven.Client.Documents.Session;
using Soundtrail.Contracts;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Search;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchStatusChanged.Ports;
using Soundtrail.Translators.Discovery;
using Soundtrail.Translators.ProjectionDocuments;

namespace Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchStatusChanged.Adapters;

public sealed class RavenLoadDiscoveryLifecycleProjection(
    IAsyncDocumentSession session,
    RavenDiscoveryLifecycleProjectionMapper mapper) : ILoadDiscoveryLifecycleProjectionPort
{
    public async Task<DiscoveryLifecycleProjection> LoadAsync(
        MusicSearchCriteria searchCriteria,
        CancellationToken cancellationToken)
    {
        var persistentId = MusicSearchTermPersistentIdTranslator.ToPersistentId(searchCriteria);
        var status = await session.LoadAsync<CatalogSearchStatusRecordDto>(
            CatalogSearchStatusRecordDto.GetDocumentId(persistentId),
            cancellationToken);
        var checkpoint = await session.LoadAsync<DiscoveryLifecycleProjectionCheckpointDocument>(
            DiscoveryLifecycleProjectionCheckpointDocument.GetDocumentId(persistentId),
            cancellationToken);
        return mapper.ToDomain(searchCriteria, status, checkpoint);
    }
}
