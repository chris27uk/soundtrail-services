using Raven.Client.Documents.Session;
using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.Persistence;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchCandidateRecorded.Ports;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchCandidateRecorded.Support;

namespace Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchCandidateRecorded.Adapters;

public sealed class RavenLoadCatalogSearchCandidateMusicTrack(IAsyncDocumentSession session) : ILoadCatalogSearchCandidateMusicTrackPort
{
    public async Task<CatalogSearchCandidateMusicTrack?> LoadAsync(
        MusicCatalogId musicCatalogId,
        CancellationToken cancellationToken)
    {
        var document = await session.LoadAsync<RavenTrackRecordDto>(
            RavenTrackRecordDto.GetDocumentId(musicCatalogId.Value),
            cancellationToken);

        return document is null
            ? null
            : new CatalogSearchCandidateMusicTrack(document.ArtistId, document.AlbumId);
    }
}
