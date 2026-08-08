using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soundtrail.Adapters.Timing;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Services.Api.Features.Catalog.GetAlbum.Adapters;
using Soundtrail.Services.Api.Features.Catalog.GetAlbum.Contract;

namespace Soundtrail.Services.Api.Features.Catalog.GetAlbum.Composition;

public sealed record GetAlbumPorts(
    Func<IServiceProvider, IGetAlbumPort> GetAlbum,
    Func<IServiceProvider, IClockPort> Clock);

public static class GetAlbumComposition
{
    public static void Configure(IServiceCollection services, GetAlbumPorts ports)
    {
        services.TryAddSingleton(ports.GetAlbum);
        services.TryAddSingleton(ports.Clock);
        services.TryAddScoped<IApiHandler<GetAlbumRequest, GetAlbumResponse?>, GetAlbumHandler>();
    }
}
