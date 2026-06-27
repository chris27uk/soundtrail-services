using Microsoft.Extensions.DependencyInjection.Extensions;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog.Browsing;

namespace Soundtrail.Services.Api.Features.GetAlbum.CompositionRoot;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddGetAlbumFeature(this IServiceCollection services)
    {
        services.TryAddScoped<IApiHandler<GetAlbumCommand, AlbumDetailsResponse?>, GetAlbumHandler>();
        return services;
    }
}
