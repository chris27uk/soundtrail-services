using Raven.Client.Documents.Session;
using Soundtrail.Domain.Search;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogCandidateIdentified.Adapters.Documents;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogCandidateIdentified.Ports;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogCandidateIdentified.Support;
using Soundtrail.Adapters.Discovery;

namespace Soundtrail.Services.Internal.Projector.Features.OnCatalogCandidateIdentified.Adapters;

public sealed class RavenLoadCatalogCandidateTracking(IAsyncDocumentSession session) : ILoadCatalogCandidateTrackingPort
{
    public async Task<CatalogCandidateTracking?> LoadAsync(
        MusicSearchCriteria searchCriteria,
        CancellationToken cancellationToken)
    {
        var document = await session.LoadAsync<CatalogSearchTrackingRecordDto>(
            CatalogSearchTrackingRecordDto.GetDocumentId(DiscoveryQueryKey.StableValueFor(searchCriteria)),
            cancellationToken);

        return document is null
            ? null
            : new CatalogCandidateTracking(
                DiscoveryQueryKey.ToMusicSearchCriteria(document.Criteria),
                document.MusicCatalogId,
                document.UpdatedAt);
    }
}
