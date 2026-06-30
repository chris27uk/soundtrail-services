using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnAssessMusicTrack.CompositionRoot;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnAlbumMetadataLookupAttempted.CompositionRoot;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnArtistMetadataLookupAttempted.CompositionRoot;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnCatalogDiscoveryWorkChanged.CompositionRoot;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnCatalogSearchRequested.CompositionRoot;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnKnownAlbumDiscoveryCompleted.CompositionRoot;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnKnownAlbumRequested.CompositionRoot;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnKnownArtistDiscoveryCompleted.CompositionRoot;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnKnownArtistRequested.CompositionRoot;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnKnownTrackRequested.CompositionRoot;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnMusicCatalogLookupHistoryChanged.CompositionRoot;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnMusicCatalogLookupAttempted.CompositionRoot;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnMusicTrackEventsImported.CompositionRoot;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnNextMusicTracksRequestedForLookup.CompositionRoot;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnStreamingLocationsRequired.CompositionRoot;
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

        services.AddOnMusicTrackEventsImportedFeature();
        services.AddOnCatalogDiscoveryWorkChangedFeature();
        services.AddOnMusicCatalogLookupAttemptedFeature();
        services.AddOnMusicCatalogLookupHistoryChangedFeature();
        services.AddOnArtistMetadataLookupAttemptedFeature();
        services.AddOnAlbumMetadataLookupAttemptedFeature();
        services.AddOnKnownArtistDiscoveryCompletedFeature();
        services.AddOnKnownAlbumDiscoveryCompletedFeature();
        services.AddOnAssessMusicTrackFeature();
        services.AddOnKnownArtistRequestedFeature();
        services.AddOnKnownAlbumRequestedFeature();
        services.AddOnKnownTrackRequestedFeature();
        services.AddOnStreamingLocationsRequiredFeature();
        services.AddOnSearchCatalogRequestedFeature(x =>
            x.ConfigureDependencies = svc => dependencyProvider.AddOnSearchCatalogRequestedDependencies(svc, configuration));
        services.AddOnNextMusicTracksRequestedForLookupFeature(
            x => x.ConfigureDependencies = svc => dependencyProvider.AddOnNextMusicTracksRequestedForLookupDependencies(svc, configuration));

        return services;
    }
}
