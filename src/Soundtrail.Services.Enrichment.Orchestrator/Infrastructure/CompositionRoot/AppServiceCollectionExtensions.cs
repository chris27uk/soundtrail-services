using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soundtrail.Services.Enrichment.Orchestrator.Features.ApplyLookupExecutionReport.CompositionRoot;
using Soundtrail.Services.Enrichment.Orchestrator.Features.BacklogScheduling.CompositionRoot;
using Soundtrail.Services.Enrichment.Orchestrator.Features.EnrichmentResponse.CompositionRoot;
using Soundtrail.Services.Enrichment.Orchestrator.Features.ImportMusicTrackEvents.CompositionRoot;
using Soundtrail.Services.Enrichment.Orchestrator.Features.JustInTimeScheduling.CompositionRoot;
using Soundtrail.Services.Enrichment.Orchestrator.Features.SchedulePlaybackReferencesLookup.CompositionRoot;
using Soundtrail.Services.Enrichment.Orchestrator.Infrastructure.Messaging;
using Soundtrail.Services.Enrichment.Orchestrator.Shared.Prioritisation;

namespace Soundtrail.Services.Enrichment.Orchestrator.Infrastructure.CompositionRoot;

public static class AppServiceCollectionExtensions
{
    public static IServiceCollection AddOrchestratorAppServices(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<OrchestratorAppServicesOptions>? configure = null)
    {
        var options = new OrchestratorAppServicesOptions();
        configure?.Invoke(options);

        services.AddOrchestratorServiceBus(configuration);
        services.TryAddSingleton<DiscoveryPriorityPolicy>();
        var dependencyProvider = options.DependencyProvider ?? new ProductionOrchestratorDependencyProvider();
        dependencyProvider.AddSharedDependencies(services, configuration);

        services.AddImportMusicTrackEventsFeature();
        services.AddApplyLookupExecutionReportFeature();
        services.AddSchedulePlaybackReferencesLookupFeature();
        services.AddJustInTimeSchedulingFeature(x =>
            x.ConfigureDependencies = svc => dependencyProvider.AddJustInTimeSchedulingDependencies(svc, configuration));
        services.AddBacklogSchedulingFeature(
            x => x.ConfigureDependencies = svc => dependencyProvider.AddBacklogSchedulingDependencies(svc, configuration));
        services.AddEnrichmentResponseFeature(x =>
            x.ConfigureDependencies = svc => dependencyProvider.AddEnrichmentResponseDependencies(svc, configuration));

        return services;
    }
}
