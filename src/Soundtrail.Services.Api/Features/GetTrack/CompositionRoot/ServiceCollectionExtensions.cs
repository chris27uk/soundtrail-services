using Microsoft.Extensions.DependencyInjection.Extensions;
using Soundtrail.Domain;
using Soundtrail.Domain.CatalogBrowsing;

namespace Soundtrail.Services.Api.Features.GetTrack.CompositionRoot;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddGetTrackFeature(this IServiceCollection services)
    {
        services.TryAddScoped<IHandler<GetTrackCommand, TrackDetailsResponse?>, GetTrackHandler>();
        return services;
    }
}
