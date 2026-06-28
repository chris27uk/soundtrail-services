using Raven.Client.Documents.Session;
using Soundtrail.Domain.Search;
using Soundtrail.Services.Internal.Projector.Features.OnMusicTrackSearchStarted.Adapters.Documents;
using Soundtrail.Services.Internal.Projector.Features.OnMusicTrackSearchStarted.Ports;
using Soundtrail.Services.Internal.Projector.Features.OnMusicTrackSearchStarted.Support;
using Soundtrail.Adapters.Discovery;

namespace Soundtrail.Services.Internal.Projector.Features.OnMusicTrackSearchStarted.Adapters;

public sealed class RavenLoadCatalogSearchStartedTracking(IAsyncDocumentSession session) : ILoadCatalogSearchStartedTrackingPort
{
    public async Task<CatalogSearchStartedTracking?> LoadAsync(
        MusicSearchCriteria searchCriteria,
        CancellationToken cancellationToken)
    {
        var document = await session.LoadAsync<CatalogSearchTrackingRecordDto>(
            CatalogSearchTrackingRecordDto.GetDocumentId(DiscoveryQueryKey.StableValueFor(searchCriteria)),
            cancellationToken);

        return document is null
            ? null
            : new CatalogSearchStartedTracking(
                DiscoveryQueryKey.ToMusicSearchCriteria(document.Criteria),
                document.MusicCatalogId,
                document.UpdatedAt);
    }
}
