using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Soundtrail.Domain.Discovery;
using Soundtrail.Services.Internal.Projector.Features.OnKnownTrackRequested.Adapters;
using Soundtrail.Services.Internal.Projector.Features.OnKnownTrackRequested.Ports;

namespace Soundtrail.Services.Internal.Projector.Features.OnKnownTrackRequested.CompositionRoot;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOnKnownTrackRequestedFeature(this IServiceCollection services)
    {
        services.TryAddScoped<ILoadKnownTrackRequestedMusicTrackPort, RavenLoadKnownTrackRequestedMusicTrack>();
        services.TryAddScoped<ICatalogSearchDiscoveryRepository, RavenKnownTrackRequestedCatalogSearchDiscoveryRepository>();
        services.TryAddScoped<KnownTrackRequestedHandler>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, KnownTrackRequestedSubscriptionHostedService>());
        return services;
    }
}
