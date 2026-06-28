using Raven.Client.Documents.Session;
using Soundtrail.Domain.Search;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchCandidateRecorded.Adapters.Documents;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchCandidateRecorded.Ports;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchCandidateRecorded.Support;
using Soundtrail.Adapters.Discovery;

namespace Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchCandidateRecorded.Adapters;

public sealed class RavenLoadCatalogSearchCandidateTracking(IAsyncDocumentSession session) : ILoadCatalogSearchCandidateTrackingPort
{
    public async Task<CatalogSearchCandidateTracking?> LoadAsync(
        MusicSearchCriteria searchCriteria,
        CancellationToken cancellationToken)
    {
        var document = await session.LoadAsync<CatalogSearchTrackingRecordDto>(
            CatalogSearchTrackingRecordDto.GetDocumentId(DiscoveryQueryKey.StableValueFor(searchCriteria)),
            cancellationToken);

        return document is null
            ? null
            : new CatalogSearchCandidateTracking(
                DiscoveryQueryKey.ToMusicSearchCriteria(document.Criteria),
                document.MusicCatalogId,
                document.UpdatedAt);
    }
}
