using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soundtrail.Adapters.Timing;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Services.Api.Features.Catalog.GetAlbumsForArtist.Adapters;
using Soundtrail.Services.Api.Features.Catalog.GetAlbumsForArtist.Contract;
using Soundtrail.Services.Api.Features.Catalog.Search.Adapters;

namespace Soundtrail.Services.Api.Features.Catalog.GetAlbumsForArtist.Composition;

public sealed record GetAlbumsForArtistPorts(
    Func<IServiceProvider, IGetAlbumsForArtistPort> GetAlbumsForArtist,
    Func<IServiceProvider, IClockPort> Clock,
    Func<IServiceProvider, ICommandBus> CommandBus,
    Func<IServiceProvider, IDiscoveryFeedbackPort> DiscoveryFeedback);

public static class GetAlbumsForArtistComposition
{
    public static void Configure(IServiceCollection services, GetAlbumsForArtistPorts ports)
    {
        services.TryAddSingleton(ports.GetAlbumsForArtist);
        services.TryAddSingleton(ports.Clock);
        services.TryAddSingleton(ports.CommandBus);
        services.TryAddSingleton(ports.DiscoveryFeedback);
        services.TryAddScoped<
            IApiHandler<GetAlbumsForArtistRequest, GetAlbumsForArtistResponse?>,
            GetAlbumsForArtistHandler>();
    }
}
