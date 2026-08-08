using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soundtrail.Adapters.Timing;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForPlaylist.Adapters;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForPlaylist.Contract;

namespace Soundtrail.Services.Api.Features.Catalog.GetTracksForPlaylist.Composition;

public sealed record GetTracksForPlaylistPorts(
    Func<IServiceProvider, IGetTracksForPlaylistPort> GetTracksForPlaylist,
    Func<IServiceProvider, IClockPort> Clock,
    Func<IServiceProvider, ICommandBus> CommandBus);

public static class GetTracksForPlaylistComposition
{
    public static void Configure(IServiceCollection services, GetTracksForPlaylistPorts ports)
    {
        services.TryAddSingleton(ports.GetTracksForPlaylist);
        services.TryAddSingleton(ports.Clock);
        services.TryAddSingleton(ports.CommandBus);
        services.TryAddScoped<
            IApiHandler<GetTracksForPlaylistRequest, GetTracksForPlaylistResponse?>,
            GetTracksForPlaylistHandler>();
    }
}
