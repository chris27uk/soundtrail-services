using Raven.Client.Documents.Session;
using Soundtrail.Domain.Search;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchPlannedForLookup.Ports;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchPlannedForLookup.Support;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchCandidateRecorded.Adapters.Documents;
using Soundtrail.Adapters.Discovery;

namespace Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchPlannedForLookup.Adapters;

public sealed class RavenLoadCatalogSearchPlannedTracking(IAsyncDocumentSession session) : ILoadCatalogSearchPlannedTrackingPort
{
    public async Task<CatalogSearchPlannedTracking?> LoadAsync(
        MusicSearchCriteria searchCriteria,
        CancellationToken cancellationToken)
    {
        var document = await session.LoadAsync<CatalogSearchTrackingRecordDto>(
            CatalogSearchTrackingRecordDto.GetDocumentId(DiscoveryQueryKey.StableValueFor(searchCriteria)),
            cancellationToken);

        return document is null
            ? null
            : new CatalogSearchPlannedTracking(document.MusicCatalogId);
    }
}
