using Microsoft.Extensions.DependencyInjection.Extensions;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog.Browsing;

namespace Soundtrail.Services.Api.Features.GetTrack.CompositionRoot;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddGetTrackFeature(this IServiceCollection services)
    {
        services.TryAddScoped<IApiHandler<GetTrackCommand, TrackDetailsResponse?>, GetTrackHandler>();
        return services;
    }
}
