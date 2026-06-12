using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.BacklogScheduling.Adapters;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Infrastructure.Scheduling;

namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.BacklogScheduling.CompositionRoot;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBacklogSchedulingFeature(
        this IServiceCollection services,
        Action<BacklogSchedulingFeatureOptions>? configure = null)
    {
        var options = new BacklogSchedulingFeatureOptions();
        configure?.Invoke(options);
        options.ConfigureDependencies?.Invoke(services);

        services.TryAddScoped<DiscoveryBacklogScheduler>();
        services.TryAddScoped<DiscoveryBacklogSchedulingListener>();
        if (options.IncludeHostedService)
        {
            services.AddHostedService<DiscoveryBacklogSchedulingHostedService>();
        }

        return services;
    }
}
