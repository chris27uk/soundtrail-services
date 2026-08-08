using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soundtrail.Adapters.Timing;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForAlbum.Adapters;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForAlbum.Contract;
using Soundtrail.Services.Api.Features.Catalog.Search.Adapters;

namespace Soundtrail.Services.Api.Features.Catalog.GetTracksForAlbum.Composition;

public sealed record GetTracksForAlbumPorts(
    Func<IServiceProvider, IGetTracksForAlbumPort> GetTracksForAlbum,
    Func<IServiceProvider, IClockPort> Clock,
    Func<IServiceProvider, ICommandBus> CommandBus,
    Func<IServiceProvider, IDiscoveryFeedbackPort> DiscoveryFeedback);

public static class GetTracksForAlbumComposition
{
    public static void Configure(IServiceCollection services, GetTracksForAlbumPorts ports)
    {
        services.TryAddSingleton(ports.GetTracksForAlbum);
        services.TryAddSingleton(ports.Clock);
        services.TryAddSingleton(ports.CommandBus);
        services.TryAddSingleton(ports.DiscoveryFeedback);
        services.TryAddScoped<
            IApiHandler<GetTracksForAlbumRequest, GetTracksForAlbumResponse?>,
            GetTracksForAlbumHandler>();
    }
}
