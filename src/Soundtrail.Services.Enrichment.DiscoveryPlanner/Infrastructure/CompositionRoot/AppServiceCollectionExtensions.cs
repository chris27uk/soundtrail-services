using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.BacklogScheduling.CompositionRoot;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.EnrichmentResponse.CompositionRoot;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.JustInTimeScheduling.CompositionRoot;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.ProjectDiscoveryLifecycle.CompositionRoot;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Infrastructure.Messaging;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Infrastructure.Scheduling;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Prioritisation;

namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Infrastructure.CompositionRoot;

public static class AppServiceCollectionExtensions
{
    public static IServiceCollection AddDiscoveryPlannerAppServices(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<DiscoveryPlannerAppServicesOptions>? configure = null)
    {
        var options = new DiscoveryPlannerAppServicesOptions();
        configure?.Invoke(options);

        services.AddSchedulerServiceBus(configuration);
        services.Configure<DiscoveryBacklogSchedulingOptions>(
            configuration.GetSection(DiscoveryBacklogSchedulingOptions.SectionName));
        services.TryAddSingleton<DiscoveryPriorityPolicy>();
        var dependencyProvider = options.DependencyProvider ?? new ProductionDiscoveryPlannerDependencyProvider();
        dependencyProvider.AddSharedDependencies(services, configuration);

        services.AddJustInTimeSchedulingFeature(x =>
            x.ConfigureDependencies = svc => dependencyProvider.AddJustInTimeSchedulingDependencies(svc, configuration));
        services.AddProjectDiscoveryLifecycleFeature();
        services.AddBacklogSchedulingFeature(x =>
        {
            x.IncludeHostedService = options.IncludeBacklogHostedService;
            x.ConfigureDependencies = svc => dependencyProvider.AddBacklogSchedulingDependencies(svc, configuration);
        });
        services.AddEnrichmentResponseFeature(x =>
            x.ConfigureDependencies = svc => dependencyProvider.AddEnrichmentResponseDependencies(svc, configuration));

        return services;
    }
}
