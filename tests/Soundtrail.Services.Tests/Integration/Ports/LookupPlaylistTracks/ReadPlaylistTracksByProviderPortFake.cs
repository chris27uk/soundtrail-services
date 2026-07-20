using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Playlists;
using Soundtrail.Domain.Common;
using Soundtrail.Services.Enrichment.Worker.Features.LookupPlaylistTracks.Ports;

namespace Soundtrail.Services.Tests.Integration.Ports.LookupPlaylistTracks;

internal sealed class ReadPlaylistTracksByProviderPortFake(
    IReadOnlyList<TrackReference>? tracks = null) : IReadPlaylistTracksByProviderPort
{
    public Task<IReadOnlyList<TrackReference>> ReadAsync(
        PlaylistId playlistId,
        ProviderName provider,
        CancellationToken cancellationToken)
    {
        if (playlistId.Value != PlaylistId.FromPlaylistName("WorldwideSongChart").Value || provider != ProviderName.Spotify)
        {
            return Task.FromResult<IReadOnlyList<TrackReference>>([]);
        }

        return Task.FromResult(tracks ?? Array.Empty<TrackReference>());
    }
}
