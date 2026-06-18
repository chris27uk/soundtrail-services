using Microsoft.Extensions.DependencyInjection.Extensions;
using Soundtrail.Domain;
using Soundtrail.Domain.CatalogBrowsing;

namespace Soundtrail.Services.Api.Features.GetArtist.CompositionRoot;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddGetArtistFeature(this IServiceCollection services)
    {
        services.TryAddScoped<IHandler<GetArtistCommand, ArtistDetailsResponse?>, GetArtistHandler>();
        return services;
    }
}
