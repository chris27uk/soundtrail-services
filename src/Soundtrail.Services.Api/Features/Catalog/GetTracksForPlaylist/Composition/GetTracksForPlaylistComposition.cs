using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soundtrail.Adapters.Timing;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForPlaylist.Adapters;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForPlaylist.Contract;

namespace Soundtrail.Services.Api.Features.Catalog.GetTracksForPlaylist.Composition;

public sealed class GetTracksForPlaylistPorts(
    Func<IServiceProvider, IGetTracksForPlaylistPort> getTracksForPlaylist,
    Func<IServiceProvider, IClockPort> clock,
    Func<IServiceProvider, ICommandBus> commandBus)
{
    public Func<IServiceProvider, IGetTracksForPlaylistPort> GetTracksForPlaylist { get; } = getTracksForPlaylist;

    public Func<IServiceProvider, IClockPort> Clock { get; } = clock;

    public Func<IServiceProvider, ICommandBus> CommandBus { get; } = commandBus;
}

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
