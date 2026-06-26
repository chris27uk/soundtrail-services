using Raven.Client.Documents.Session;
using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.Persistence;
using Soundtrail.Services.Internal.Projector.Features.OnMusicTrackSearchStarted.Ports;
using Soundtrail.Services.Internal.Projector.Features.OnMusicTrackSearchStarted.Support;

namespace Soundtrail.Services.Internal.Projector.Features.OnMusicTrackSearchStarted.Adapters;

public sealed class RavenLoadCatalogSearchStartedMusicTrack(IAsyncDocumentSession session) : ILoadCatalogSearchStartedMusicTrackPort
{
    public async Task<CatalogSearchStartedMusicTrack?> LoadAsync(
        MusicCatalogId musicCatalogId,
        CancellationToken cancellationToken)
    {
        var document = await session.LoadAsync<RavenTrackRecordDto>(
            RavenTrackRecordDto.GetDocumentId(musicCatalogId.Value),
            cancellationToken);

        return document is null
            ? null
            : new CatalogSearchStartedMusicTrack(document.ArtistId, document.AlbumId);
    }
}
