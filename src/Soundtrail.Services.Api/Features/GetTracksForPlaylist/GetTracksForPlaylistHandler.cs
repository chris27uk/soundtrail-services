using Soundtrail.Domain.Abstractions;
using Soundtrail.Services.Api.Features.GetTracksForPlaylist.Adapters;
using Soundtrail.Services.Api.Features.GetTracksForPlaylist.Contract;

namespace Soundtrail.Services.Api.Features.GetTracksForPlaylist;

public sealed class GetTracksForPlaylistHandler(IGetTracksForPlaylistPort getTracksForPlaylistPort) : IApiHandler<GetTracksForPlaylistRequest, GetTracksForPlaylistResponse?>
{
    public async Task<GetTracksForPlaylistResponse?> Handle(GetTracksForPlaylistRequest request, CancellationToken cancellationToken = default)
    {
        return await getTracksForPlaylistPort.GetTracksForPlaylistAsync(request.PlaylistId, cancellationToken);
    }
}
