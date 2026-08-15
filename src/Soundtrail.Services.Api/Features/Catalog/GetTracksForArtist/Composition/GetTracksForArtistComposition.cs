using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soundtrail.Adapters.Timing;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForArtist.Adapters;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForArtist.Contract;
using Soundtrail.Services.Api.Features.Catalog.Search.Adapters;

namespace Soundtrail.Services.Api.Features.Catalog.GetTracksForArtist.Composition;

public sealed record GetTracksForArtistPorts(
    Func<IServiceProvider, IGetTracksForArtistPort> GetTracksForArtist,
    Func<IServiceProvider, IClockPort> Clock,
    Func<IServiceProvider, ICommandBus> CommandBus,
    Func<IServiceProvider, IDiscoveryFeedbackPort> DiscoveryFeedback);

public static class GetTracksForArtistComposition
{
    public static void Configure(IServiceCollection services, GetTracksForArtistPorts ports)
    {
        services.TryAddScoped(ports.GetTracksForArtist);
        services.TryAddScoped(ports.Clock);
        services.TryAddScoped(ports.CommandBus);
        services.TryAddScoped(ports.DiscoveryFeedback);
        services.TryAddScoped<
            IApiHandler<GetTracksForArtistRequest, GetTracksForArtistResponse?>,
            GetTracksForArtistHandler>();
    }
}
