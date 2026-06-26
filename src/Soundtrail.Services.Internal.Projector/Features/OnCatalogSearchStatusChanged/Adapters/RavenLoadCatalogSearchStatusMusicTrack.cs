using Raven.Client.Documents.Session;
using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.Persistence;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchStatusChanged.Ports;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchStatusChanged.Support;

namespace Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchStatusChanged.Adapters;

public sealed class RavenLoadCatalogSearchStatusMusicTrack(IAsyncDocumentSession session) : ILoadCatalogSearchStatusMusicTrackPort
{
    public async Task<CatalogSearchStatusMusicTrack?> LoadAsync(
        MusicCatalogId musicCatalogId,
        CancellationToken cancellationToken)
    {
        var document = await session.LoadAsync<RavenTrackRecordDto>(
            RavenTrackRecordDto.GetDocumentId(musicCatalogId.Value),
            cancellationToken);

        return document is null
            ? null
            : new CatalogSearchStatusMusicTrack(
                document.ArtistId,
                document.AlbumId,
                document.IsPlayable,
                document.Isrc,
                document.ResolvedMetadata?.Isrc,
                document.Title,
                document.ResolvedMetadata?.Title,
                document.Artist,
                document.ResolvedMetadata?.Artist,
                document.AlbumTitle);
    }
}
