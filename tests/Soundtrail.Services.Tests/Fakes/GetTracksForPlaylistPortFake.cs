using Soundtrail.Domain.Catalog.Playlists;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForPlaylist.Adapters;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForPlaylist.Contract;

namespace Soundtrail.Services.Tests.Fakes;

internal sealed class GetTracksForPlaylistPortFake : IGetTracksForPlaylistPort
{
    private readonly Dictionary<PlaylistId, GetTracksForPlaylistResponse> responses = [];
    private readonly Func<PlaylistId, CancellationToken, Task<GetTracksForPlaylistResponse?>>? resolver;

    private GetTracksForPlaylistPortFake(
        Func<PlaylistId, CancellationToken, Task<GetTracksForPlaylistResponse?>>? resolver = null)
    {
        this.resolver = resolver;
    }

    public List<PlaylistId> RequestedPlaylistIds { get; } = [];

    public static GetTracksForPlaylistPortFake Create() => new();

    public static GetTracksForPlaylistPortFake Create(
        Func<PlaylistId, CancellationToken, Task<GetTracksForPlaylistResponse?>> resolver) => new(resolver);

    public GetTracksForPlaylistPortFake WithPlaylistTracks(GetTracksForPlaylistResponse response)
    {
        responses[response.PlaylistId] = response;
        return this;
    }

    public Task<GetTracksForPlaylistResponse?> GetTracksForPlaylistAsync(
        PlaylistId playlistId,
        CancellationToken cancellationToken)
    {
        RequestedPlaylistIds.Add(playlistId);
        return resolver is null
            ? Task.FromResult(responses.GetValueOrDefault(playlistId))
            : resolver(playlistId, cancellationToken);
    }
}
