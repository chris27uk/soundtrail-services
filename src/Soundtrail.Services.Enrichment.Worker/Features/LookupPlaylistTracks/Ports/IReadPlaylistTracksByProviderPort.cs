using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Playlists;
using Soundtrail.Domain.Common;

namespace Soundtrail.Services.Enrichment.Worker.Features.LookupPlaylistTracks.Ports;

public interface IReadPlaylistTracksByProviderPort
{
    Task<IReadOnlyList<TrackReference>> ReadAsync(
        PlaylistId playlistId,
        ProviderName provider,
        CancellationToken cancellationToken);
}
