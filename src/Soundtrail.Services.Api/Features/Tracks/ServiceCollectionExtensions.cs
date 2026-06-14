using Microsoft.Extensions.DependencyInjection.Extensions;
using Soundtrail.Domain;
using Soundtrail.Domain.CatalogBrowsing;

namespace Soundtrail.Services.Api.Features.Tracks;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTracksFeature(this IServiceCollection services)
    {
        services.TryAddScoped<IHandler<GetTrackCommand, TrackDetailsResponse?>, GetTrackHandler>();
        return services;
    }
}
