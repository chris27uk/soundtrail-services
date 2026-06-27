using Raven.Client.Documents.Session;
using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.Persistence;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchPlannedForLookup.Ports;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchPlannedForLookup.Support;

namespace Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchPlannedForLookup.Adapters;

public sealed class RavenLoadCatalogSearchPlannedMusicTrack(IAsyncDocumentSession session) : ILoadCatalogSearchPlannedMusicTrackPort
{
    public async Task<CatalogSearchPlannedMusicTrack?> LoadAsync(
        MusicCatalogId musicCatalogId,
        CancellationToken cancellationToken)
    {
        var document = await session.LoadAsync<RavenTrackRecordDto>(
            RavenTrackRecordDto.GetDocumentId(musicCatalogId.Value),
            cancellationToken);

        return document is null
            ? null
            : new CatalogSearchPlannedMusicTrack(
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
