using Microsoft.Extensions.DependencyInjection.Extensions;
using Soundtrail.Domain;
using Soundtrail.Domain.CatalogBrowsing;

namespace Soundtrail.Services.Api.Features.Artists;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddArtistsFeature(this IServiceCollection services)
    {
        services.TryAddScoped<IHandler<GetArtistCommand, ArtistDetailsResponse?>, GetArtistHandler>();
        services.TryAddScoped<IHandler<ListTracksByArtistCommand, ArtistTracksResponse?>, ListTracksByArtistHandler>();
        return services;
    }
}
