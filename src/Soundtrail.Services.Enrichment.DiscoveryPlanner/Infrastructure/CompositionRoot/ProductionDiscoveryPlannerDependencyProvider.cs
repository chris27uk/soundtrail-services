using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soundtrail.Domain.Discovery;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Infrastructure.Raven;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.SourceBudgets.Adapters;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.SourceBudgets.Configuration;

namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Infrastructure.CompositionRoot;

public sealed class ProductionDiscoveryPlannerDependencyProvider : IDiscoveryPlannerDependencyProvider
{
    public void AddSharedDependencies(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSchedulerRavenDocumentStore(configuration);
        services.Configure<SourceApiBudgetsOptions>(configuration.GetSection(SourceApiBudgetsOptions.SectionName));
        services.TryAddScoped<IReserveSourceApiBudgetPort, RavenCompareExchangeSourceApiBudgetPort>();
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
