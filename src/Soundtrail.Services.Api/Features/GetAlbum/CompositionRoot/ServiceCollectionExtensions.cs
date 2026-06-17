using Microsoft.Extensions.DependencyInjection.Extensions;
using Soundtrail.Domain;
using Soundtrail.Domain.CatalogBrowsing;

namespace Soundtrail.Services.Api.Features.Albums.GetAlbum.CompositionRoot;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddGetAlbumFeature(this IServiceCollection services)
    {
        services.TryAddScoped<IHandler<GetAlbumCommand, AlbumDetailsResponse?>, GetAlbumHandler>();
        return services;
    }
}
