using Microsoft.Extensions.DependencyInjection.Extensions;
using Soundtrail.Domain;
using Soundtrail.Domain.CatalogBrowsing;

namespace Soundtrail.Services.Api.Features.Albums;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAlbumsFeature(this IServiceCollection services)
    {
        services.TryAddScoped<IHandler<GetAlbumCommand, AlbumDetailsResponse?>, GetAlbumHandler>();
        services.TryAddScoped<IHandler<ListTracksByAlbumCommand, AlbumTracksResponse?>, ListTracksByAlbumHandler>();
        return services;
    }
}
