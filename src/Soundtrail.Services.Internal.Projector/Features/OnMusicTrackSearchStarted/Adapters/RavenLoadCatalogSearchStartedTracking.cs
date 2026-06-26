using Raven.Client.Documents.Session;
using Soundtrail.Domain.Discovery;
using Soundtrail.Services.Internal.Projector.Features.OnMusicTrackSearchStarted.Adapters.Documents;
using Soundtrail.Services.Internal.Projector.Features.OnMusicTrackSearchStarted.Ports;
using Soundtrail.Services.Internal.Projector.Features.OnMusicTrackSearchStarted.Support;

namespace Soundtrail.Services.Internal.Projector.Features.OnMusicTrackSearchStarted.Adapters;

public sealed class RavenLoadCatalogSearchStartedTracking(IAsyncDocumentSession session) : ILoadCatalogSearchStartedTrackingPort
{
    public async Task<CatalogSearchStartedTracking?> LoadAsync(
        CatalogSearchCriteria criteria,
        CancellationToken cancellationToken)
    {
        var document = await session.LoadAsync<CatalogSearchTrackingRecordDto>(
            CatalogSearchTrackingRecordDto.GetDocumentId(criteria.Value),
            cancellationToken);

        return document is null
            ? null
            : new CatalogSearchStartedTracking(document.Criteria, document.MusicCatalogId, document.UpdatedAt);
    }
}
