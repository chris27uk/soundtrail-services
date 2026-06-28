using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Soundtrail.Services.Internal.Projector.Features.OnDiscoveryRequested.Adapters;

namespace Soundtrail.Services.Internal.Projector.Features.OnDiscoveryRequested.CompositionRoot;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOnDiscoveryRequestedFeature(this IServiceCollection services)
    {
        services.TryAddScoped<OnDiscoveryRequestedHandler>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, DiscoveryRequestedSubscriptionHostedService>());
        return services;
    }
}
