using Microsoft.Extensions.DependencyInjection.Extensions;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog.Browsing;

namespace Soundtrail.Services.Api.Features.GetArtist.CompositionRoot;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddGetArtistFeature(this IServiceCollection services)
    {
        services.TryAddScoped<IApiHandler<GetArtistCommand, ArtistDetailsResponse?>, GetArtistHandler>();
        return services;
    }
}
