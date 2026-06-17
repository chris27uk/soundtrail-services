using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Infrastructure.Raven;

namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Infrastructure.CompositionRoot;

public sealed class ProductionDiscoveryPlannerDependencyProvider : IDiscoveryPlannerDependencyProvider
{
    public void AddSharedDependencies(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSchedulerRavenDocumentStore(configuration);
    }

    public void AddJustInTimeSchedulingDependencies(IServiceCollection services, IConfiguration configuration)
    {
    }

    public void AddBacklogSchedulingDependencies(IServiceCollection services, IConfiguration configuration)
    {
    }

    public void AddEnrichmentResponseDependencies(IServiceCollection services, IConfiguration configuration)
    {
    }
}
