using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soundtrail.Services.Enrichment.Worker.Features.OnLookupStreamingLocations.Adapters;
using Soundtrail.Services.Enrichment.Worker.Features.OnLookupStreamingLocations.Pipeline;
using Soundtrail.Services.Enrichment.Worker.Infrastructure.Idempotency.Storage;
using Soundtrail.Domain.Discovery;

namespace Soundtrail.Services.Enrichment.Worker.Features.OnLookupStreamingLocations.CompositionRoot;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddLookupStreamingLocationsFeature(
        this IServiceCollection services,
        Action<LookupStreamingLocationsFeatureOptions>? configure = null)
    {
        var options = new LookupStreamingLocationsFeatureOptions();
        configure?.Invoke(options);
        options.ConfigureDependencies?.Invoke(services);

        services.TryAddScoped<LookupStreamingLocationsHandler>();
        services.TryAddScoped<LookupStreamingLocationsExecutionAdmissionDecorator>(sp =>
            new LookupStreamingLocationsExecutionAdmissionDecorator(
                sp.GetRequiredService<Shared.ExecutionAdmission.ILookupExecutionAdmissionPort>(),
                sp.GetRequiredService<LookupStreamingLocationsHandler>()));
        services.TryAddScoped<ILookupStreamingLocationsHandler>(sp =>
            sp.GetRequiredService<LookupStreamingLocationsExecutionAdmissionDecorator>());
        services.TryAddScoped<LookupStreamingLocationsListener>();
        return services;
    }
}
