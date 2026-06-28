using Raven.Client.Documents.Session;
using Soundtrail.Domain.Search;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchStatusChanged.Ports;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchStatusChanged.Support;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogCandidateIdentified.Adapters.Documents;
using Soundtrail.Adapters.Discovery;

namespace Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchStatusChanged.Adapters;

public sealed class RavenLoadCatalogSearchStatusTracking(IAsyncDocumentSession session) : ILoadCatalogSearchStatusTrackingPort
{
    public async Task<CatalogSearchStatusTracking?> LoadAsync(
        MusicSearchCriteria searchCriteria,
        CancellationToken cancellationToken)
    {
        var document = await session.LoadAsync<CatalogSearchTrackingRecordDto>(
            CatalogSearchTrackingRecordDto.GetDocumentId(DiscoveryQueryKey.StableValueFor(searchCriteria)),
            cancellationToken);

        return document is null
            ? null
            : new CatalogSearchStatusTracking(document.MusicCatalogId);
    }
}
