using Microsoft.Extensions.DependencyInjection.Extensions;
using Soundtrail.Domain;
using Soundtrail.Domain.CatalogBrowsing;

namespace Soundtrail.Services.Api.Features.ListTracksByAlbum.CompositionRoot;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddListTracksByAlbumFeature(this IServiceCollection services)
    {
        services.TryAddScoped<IApiHandler<ListTracksByAlbumCommand, AlbumTracksResponse?>, ListTracksByAlbumHandler>();
        return services;
    }
}
