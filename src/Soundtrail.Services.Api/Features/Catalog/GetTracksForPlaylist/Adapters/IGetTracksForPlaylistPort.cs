using Soundtrail.Domain.Catalog.Playlists;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForPlaylist.Contract;

namespace Soundtrail.Services.Api.Features.Catalog.GetTracksForPlaylist.Adapters;

public interface IGetTracksForPlaylistPort
{
    Task<GetTracksForPlaylistResponse?> GetTracksForPlaylistAsync(PlaylistId playlistId, CancellationToken cancellationToken);
}
