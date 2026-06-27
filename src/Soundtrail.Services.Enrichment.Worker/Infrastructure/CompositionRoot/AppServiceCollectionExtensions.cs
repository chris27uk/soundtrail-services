using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Soundtrail.Services.Enrichment.Worker.Infrastructure.Messaging;
using Soundtrail.Services.Enrichment.Worker.Features.OnLookupMusicMetadata.CompositionRoot;
using Soundtrail.Services.Enrichment.Worker.Features.OnLookupStreamingLocations.CompositionRoot;

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
        services.AddLookupStreamingLocationsFeature(x =>
            x.ConfigureDependencies = svc => dependencyProvider.AddLookupStreamingLocationsDependencies(svc, configuration));

        return services;
    }
}
