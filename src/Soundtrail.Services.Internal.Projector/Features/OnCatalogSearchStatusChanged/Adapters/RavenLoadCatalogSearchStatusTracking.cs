using Raven.Client.Documents.Session;
using Soundtrail.Domain.Discovery;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchStatusChanged.Ports;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchStatusChanged.Support;
using Soundtrail.Services.Internal.Projector.Features.OnMusicTrackSearchStarted.Adapters.Documents;

namespace Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchStatusChanged.Adapters;

public sealed class RavenLoadCatalogSearchStatusTracking(IAsyncDocumentSession session) : ILoadCatalogSearchStatusTrackingPort
{
    public async Task<CatalogSearchStatusTracking?> LoadAsync(
        CatalogSearchCriteria criteria,
        CancellationToken cancellationToken)
    {
        var document = await session.LoadAsync<CatalogSearchTrackingRecordDto>(
            CatalogSearchTrackingRecordDto.GetDocumentId(criteria.Value),
            cancellationToken);

        return document is null
            ? null
            : new CatalogSearchStatusTracking(document.MusicCatalogId);
    }
}
