using Raven.Client.Documents.Session;
using Soundtrail.Domain.Discovery;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchPlannedForLookup.Ports;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchPlannedForLookup.Support;
using Soundtrail.Services.Internal.Projector.Features.OnMusicTrackSearchStarted.Adapters.Documents;

namespace Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchPlannedForLookup.Adapters;

public sealed class RavenLoadCatalogSearchPlannedTracking(IAsyncDocumentSession session) : ILoadCatalogSearchPlannedTrackingPort
{
    public async Task<CatalogSearchPlannedTracking?> LoadAsync(
        CatalogSearchCriteria criteria,
        CancellationToken cancellationToken)
    {
        var document = await session.LoadAsync<CatalogSearchTrackingRecordDto>(
            CatalogSearchTrackingRecordDto.GetDocumentId(criteria.Value),
            cancellationToken);

        return document is null
            ? null
            : new CatalogSearchPlannedTracking(document.MusicCatalogId);
    }
}
