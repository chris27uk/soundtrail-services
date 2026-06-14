using Microsoft.Extensions.DependencyInjection.Extensions;
using Soundtrail.Domain;
using Soundtrail.Domain.CatalogBrowsing;

namespace Soundtrail.Services.Api.Features.Artists.ListTracksByArtist.CompositionRoot;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddListTracksByArtistFeature(this IServiceCollection services)
    {
        services.TryAddScoped<IHandler<ListTracksByArtistCommand, ArtistTracksResponse?>, ListTracksByArtistHandler>();
        return services;
    }
}
