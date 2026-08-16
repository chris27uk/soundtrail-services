using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soundtrail.Adapters.Timing;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Services.Api.Features.Catalog.GetTrack.Adapters;
using Soundtrail.Services.Api.Features.Catalog.GetTrack.Contract;

namespace Soundtrail.Services.Api.Features.Catalog.GetTrack.Composition;

public sealed record GetTrackPorts(
    Func<IServiceProvider, IGetTrackPort> GetTrack,
    Func<IServiceProvider, IClockPort> Clock,
    Func<IServiceProvider, ICommandBus> CommandBus);

public static class GetTrackComposition
{
    public static void Configure(IServiceCollection services, GetTrackPorts ports)
    {
        services.TryAddScoped(ports.GetTrack);
        services.TryAddScoped(ports.Clock);
        services.TryAddScoped(ports.CommandBus);
        services.TryAddScoped<IApiHandler<GetTrackRequest, GetTrackResponse?>, GetTrackHandler>();
    }
}
