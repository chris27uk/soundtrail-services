using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Soundtrail.Services.Internal.Projector.Features.OnMusicTrackSearchStarted.Adapters;

namespace Soundtrail.Services.Internal.Projector.Features.OnMusicTrackSearchStarted.CompositionRoot;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOnMusicTrackSearchStartedFeature(this IServiceCollection services)
    {
        services.TryAddScoped<MusicTrackSearchStartedHandler>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, MusicTrackSearchStartedSubscriptionHostedService>());
        return services;
    }
}
