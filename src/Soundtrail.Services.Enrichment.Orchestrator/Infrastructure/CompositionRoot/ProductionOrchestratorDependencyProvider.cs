using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soundtrail.Domain.Discovery;
using Soundtrail.Services.Enrichment.Orchestrator.Infrastructure.Raven;
using Soundtrail.Services.Enrichment.Orchestrator.Shared.SourceBudgets;
using Soundtrail.Services.Enrichment.Orchestrator.Shared.SourceBudgets.Adapters;
using Soundtrail.Services.Enrichment.Orchestrator.Shared.SourceBudgets.Configuration;

namespace Soundtrail.Services.Enrichment.Orchestrator.Infrastructure.CompositionRoot;

public sealed class ProductionOrchestratorDependencyProvider : IOrchestratorDependencyProvider
{
    public void AddSharedDependencies(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSchedulerRavenDocumentStore(configuration);
        services.Configure<SourceApiBudgetsOptions>(configuration.GetSection(SourceApiBudgetsOptions.SectionName));
        services.TryAddScoped<ITryReserveSourceApiBudgetWindowPort, RavenCompareExchangeSourceApiBudgetPort>();
        services.TryAddScoped<IReserveSourceApiBudgetPort, SourceApiBudgetReservationService>();
    }

    public void AddOnSearchCatalogRequestedDependencies(IServiceCollection services, IConfiguration configuration)
    {
    }

    public void AddOnNextMusicTracksRequestedForLookupDependencies(IServiceCollection services, IConfiguration configuration)
    {
    }
}
