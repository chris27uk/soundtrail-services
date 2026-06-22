using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soundtrail.Services.Enrichment.Orchestrator.Features.BacklogScheduling.Adapters;
using Soundtrail.Services.Enrichment.Orchestrator.Features.BacklogScheduling.Support;
using Soundtrail.Services.Enrichment.Orchestrator.Infrastructure.Scheduling;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.BacklogScheduling.CompositionRoot;

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
        services.TryAddScoped<DiscoveryBacklogLookupPlanner>();
        services.TryAddScoped<TrackedDiscoveryStartMarker>();
        services.TryAddScoped<DiscoveryBacklogSchedulingListener>();
        if (options.IncludeHostedService)
        {
            services.AddHostedService<DiscoveryBacklogSchedulingHostedService>();
        }

        return services;
    }
}
