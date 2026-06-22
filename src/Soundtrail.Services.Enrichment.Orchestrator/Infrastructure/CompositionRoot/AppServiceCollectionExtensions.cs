using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soundtrail.Services.Enrichment.Orchestrator.Features.ApplyLookupExecutionReport.CompositionRoot;
using Soundtrail.Services.Enrichment.Orchestrator.Features.BacklogScheduling.CompositionRoot;
using Soundtrail.Services.Enrichment.Orchestrator.Features.EnrichmentResponse.CompositionRoot;
using Soundtrail.Services.Enrichment.Orchestrator.Features.ImportCatalogSearchDiscoveryEvents.CompositionRoot;
using Soundtrail.Services.Enrichment.Orchestrator.Features.ImportMusicTrackEvents.CompositionRoot;
using Soundtrail.Services.Enrichment.Orchestrator.Features.JustInTimeScheduling.CompositionRoot;
using Soundtrail.Services.Enrichment.Orchestrator.Features.ProjectDiscoveryLifecycle.CompositionRoot;
using Soundtrail.Services.Enrichment.Orchestrator.Features.ProjectMusicTrackProjection.CompositionRoot;
using Soundtrail.Services.Enrichment.Orchestrator.Features.ReplayDiscoveryLifecycleProjection.CompositionRoot;
using Soundtrail.Services.Enrichment.Orchestrator.Features.ReplayMusicTrackProjection.CompositionRoot;
using Soundtrail.Services.Enrichment.Orchestrator.Features.SchedulePlaybackReferencesLookup.CompositionRoot;
using Soundtrail.Services.Enrichment.Orchestrator.Infrastructure.Messaging;
using Soundtrail.Services.Enrichment.Orchestrator.Infrastructure.Scheduling;
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
        services.Configure<DiscoveryBacklogSchedulingOptions>(
            configuration.GetSection(DiscoveryBacklogSchedulingOptions.SectionName));
        services.TryAddSingleton<DiscoveryPriorityPolicy>();
        var dependencyProvider = options.DependencyProvider ?? new ProductionOrchestratorDependencyProvider();
        dependencyProvider.AddSharedDependencies(services, configuration);

        services.AddImportMusicTrackEventsFeature();
        services.AddImportCatalogSearchDiscoveryEventsFeature();
        services.AddApplyLookupExecutionReportFeature();
        services.AddSchedulePlaybackReferencesLookupFeature();
        services.AddJustInTimeSchedulingFeature(x =>
            x.ConfigureDependencies = svc => dependencyProvider.AddJustInTimeSchedulingDependencies(svc, configuration));
        services.AddProjectDiscoveryLifecycleFeature();
        services.AddReplayDiscoveryLifecycleProjectionFeature();
        services.AddProjectMusicTrackProjectionFeature();
        services.AddReplayMusicTrackProjectionFeature();
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
