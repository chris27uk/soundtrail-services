using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Soundtrail.Services.Internal.Projector.Features.OnMusicTrackChanged.Adapters;
using Soundtrail.Adapters.ProjectionDocuments;

namespace Soundtrail.Services.Internal.Projector.Features.OnMusicTrackChanged.CompositionRoot;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOnMusicTrackChangedFeature(this IServiceCollection services)
    {
        services.TryAddScoped<MusicTrackChangedHandler>();
        services.TryAddSingleton<RavenMusicTrackProjectionMapper>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, MusicTrackChangedSubscriptionHostedService>());
        return services;
    }
}
