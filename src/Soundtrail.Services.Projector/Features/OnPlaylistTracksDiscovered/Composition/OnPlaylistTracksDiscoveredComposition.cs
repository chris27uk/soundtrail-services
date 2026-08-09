using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soundtrail.Services.Internal.Projector.Features.OnPlaylistTracksDiscovered.Adapters;

namespace Soundtrail.Services.Internal.Projector.Features.OnPlaylistTracksDiscovered.Composition;

public sealed record OnPlaylistTracksDiscoveredPorts(
    Func<IServiceProvider, IStorePlaylistTracksReadModelPort> PlaylistTracks);

public static class OnPlaylistTracksDiscoveredComposition
{
    public static void Configure(IServiceCollection services, OnPlaylistTracksDiscoveredPorts ports)
    {
        services.TryAddScoped(ports.PlaylistTracks);
    }
}
