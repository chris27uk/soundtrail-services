using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Soundtrail.Services.Enrichment.Worker.Features.OnLookupAlbumMetadata.CompositionRoot;
using Soundtrail.Services.Enrichment.Worker.Features.OnLookupArtistMetadata.CompositionRoot;
using Soundtrail.Services.Enrichment.Worker.Features.OnLookupTrackMetadata.CompositionRoot;
using Soundtrail.Services.Enrichment.Worker.Features.OnLookupStreamingLocations.CompositionRoot;
using Soundtrail.Services.Enrichment.Worker.Infrastructure.Messaging;

namespace Soundtrail.Services.Enrichment.Worker.Infrastructure.CompositionRoot;

public static class AppServiceCollectionExtensions
{
    public static IServiceCollection AddWorkerAppServices(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<WorkerAppServicesOptions>? configure = null)
    {
        services.AddWorkerServiceBus(configuration);
        var options = new WorkerAppServicesOptions();
        configure?.Invoke(options);
        var dependencyProvider = options.DependencyProvider ?? new ProductionWorkerDependencyProvider();
        dependencyProvider.AddSharedDependencies(services, configuration);

        services.AddLookupTrackMetadataFeature(x =>
            x.ConfigureDependencies = svc => dependencyProvider.AddLookupTrackMetadataDependencies(svc, configuration));
        dependencyProvider.AddLookupArtistMetadataDependencies(services, configuration);
        dependencyProvider.AddLookupAlbumMetadataDependencies(services, configuration);
        services.AddLookupArtistMetadataFeature();
        services.AddLookupAlbumMetadataFeature();
        services.AddLookupStreamingLocationsFeature(x =>
            x.ConfigureDependencies = svc => dependencyProvider.AddLookupStreamingLocationsDependencies(svc, configuration));

        return services;
    }
}
