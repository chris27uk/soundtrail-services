using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Services.Api.Features.Catalog.GetTrack.Adapters;
using Soundtrail.Services.Api.Features.Catalog.GetTrack.Contract;

namespace Soundtrail.Services.Api.Features.Catalog.GetTrack.Composition;

public sealed record GetTrackPorts(Func<IServiceProvider, IGetTrackPort> GetTrack);

public static class GetTrackComposition
{
    public static void Configure(IServiceCollection services, GetTrackPorts ports)
    {
        services.TryAddSingleton(ports.GetTrack);
        services.TryAddScoped<IApiHandler<GetTrackRequest, GetTrackResponse?>, GetTrackHandler>();
    }
}
