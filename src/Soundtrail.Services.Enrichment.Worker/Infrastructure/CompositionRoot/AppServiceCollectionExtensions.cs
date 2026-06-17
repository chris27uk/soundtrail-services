using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Soundtrail.Services.Enrichment.Worker.Infrastructure.Messaging;
using Soundtrail.Services.Enrichment.Worker.Features.OnDemandMetadataLookup.CompositionRoot;
using Soundtrail.Services.Enrichment.Worker.Features.PlaybackReferencesLookupExecution.CompositionRoot;

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

        services.AddOnDemandMetadataLookupFeature(x =>
            x.ConfigureDependencies = svc => dependencyProvider.AddOnDemandMetadataLookupDependencies(svc, configuration));
        services.AddPlaybackReferencesLookupExecutionFeature(x =>
            x.ConfigureDependencies = svc => dependencyProvider.AddPlaybackReferencesLookupDependencies(svc, configuration));

        return services;
    }
}
