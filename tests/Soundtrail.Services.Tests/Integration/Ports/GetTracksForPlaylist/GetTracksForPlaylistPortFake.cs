using Soundtrail.Domain.Catalog.Playlists;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForPlaylist.Adapters;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForPlaylist.Contract;

namespace Soundtrail.Services.Tests.Integration.Ports.GetTracksForPlaylist;

internal sealed class GetTracksForPlaylistPortFake(GetTracksForPlaylistResponse? response = null) : IGetTracksForPlaylistPort
{
    public Task<GetTracksForPlaylistResponse?> GetTracksForPlaylistAsync(PlaylistId playlistId, CancellationToken cancellationToken) => Task.FromResult(response);
}
