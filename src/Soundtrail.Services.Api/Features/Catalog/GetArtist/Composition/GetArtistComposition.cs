using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Services.Api.Features.Catalog.GetArtist.Adapters;
using Soundtrail.Services.Api.Features.Catalog.GetArtist.Contract;

namespace Soundtrail.Services.Api.Features.Catalog.GetArtist.Composition;

public sealed record GetArtistPorts(Func<IServiceProvider, IGetArtistPort> GetArtist);

public static class GetArtistComposition
{
    public static void Configure(IServiceCollection services, GetArtistPorts ports)
    {
        services.TryAddScoped(ports.GetArtist);
        services.TryAddScoped<IApiHandler<GetArtistRequest, GetArtistResponse?>, GetArtistHandler>();
    }
}
