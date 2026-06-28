using Raven.Client.Documents.Session;
using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.Persistence;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogCandidateIdentified.Ports;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogCandidateIdentified.Support;

namespace Soundtrail.Services.Internal.Projector.Features.OnCatalogCandidateIdentified.Adapters;

public sealed class RavenLoadCatalogCandidateMusicTrack(IAsyncDocumentSession session) : ILoadCatalogCandidateMusicTrackPort
{
    public async Task<CatalogCandidateMusicTrack?> LoadAsync(
        MusicCatalogId musicCatalogId,
        CancellationToken cancellationToken)
    {
        var document = await session.LoadAsync<RavenTrackRecordDto>(
            RavenTrackRecordDto.GetDocumentId(musicCatalogId.Value),
            cancellationToken);

        return document is null
            ? null
            : new CatalogCandidateMusicTrack(document.ArtistId, document.AlbumId);
    }
}
