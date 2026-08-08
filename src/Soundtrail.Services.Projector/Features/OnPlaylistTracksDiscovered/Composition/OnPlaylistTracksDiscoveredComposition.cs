using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soundtrail.Services.Internal.Projector.Features.OnPlaylistTracksDiscovered.Adapters;

namespace Soundtrail.Services.Internal.Projector.Features.OnPlaylistTracksDiscovered.Composition;

public sealed class OnPlaylistTracksDiscoveredPorts(
    Func<IServiceProvider, IStorePlaylistTracksReadModelPort> playlistTracks)
{
    public Func<IServiceProvider, IStorePlaylistTracksReadModelPort> PlaylistTracks { get; } = playlistTracks;
}

public static class OnPlaylistTracksDiscoveredComposition
{
    public static void Configure(IServiceCollection services, OnPlaylistTracksDiscoveredPorts ports)
    {
        services.TryAddSingleton(ports.PlaylistTracks);
    }
}
