using Raven.Client.Documents.Session;
using Soundtrail.Domain.Model;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchStatusChanged.Ports;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchStatusChanged.Support;
using Soundtrail.Services.Internal.Projector.Features.OnMusicTrackSearchStarted.Adapters.Documents;
using Soundtrail.Translators.Discovery;

namespace Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchStatusChanged.Adapters;

public sealed class RavenLoadCatalogSearchStatusTracking(IAsyncDocumentSession session) : ILoadCatalogSearchStatusTrackingPort
{
    public async Task<CatalogSearchStatusTracking?> LoadAsync(
        MusicSearchCriteria searchCriteria,
        CancellationToken cancellationToken)
    {
        var document = await session.LoadAsync<CatalogSearchTrackingRecordDto>(
            CatalogSearchTrackingRecordDto.GetDocumentId(MusicSearchTermPersistentIdTranslator.ToPersistentId(searchCriteria)),
            cancellationToken);

        return document is null
            ? null
            : new CatalogSearchStatusTracking(document.MusicCatalogId);
    }
}
