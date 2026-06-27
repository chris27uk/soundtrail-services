using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soundtrail.Services.Enrichment.Worker.Features.OnLookupMusicMetadata.Adapters;
using Soundtrail.Services.Enrichment.Worker.Features.OnLookupMusicMetadata.Pipeline;
using Soundtrail.Services.Enrichment.Worker.Infrastructure.Idempotency.Storage;
using Soundtrail.Domain.Discovery;

namespace Soundtrail.Services.Enrichment.Worker.Features.OnLookupMusicMetadata.CompositionRoot;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddLookupTrackMetadataFeature(
        this IServiceCollection services,
        Action<LookupTrackMetadataFeatureOptions>? configure = null)
    {
        var options = new LookupTrackMetadataFeatureOptions();
        configure?.Invoke(options);
        options.ConfigureDependencies?.Invoke(services);

        services.TryAddScoped<LookupTrackMetadataHandler>();
        services.TryAddScoped<LookupTrackMetadataExecutionAdmissionDecorator>(sp =>
            new LookupTrackMetadataExecutionAdmissionDecorator(
                sp.GetRequiredService<Shared.ExecutionAdmission.ILookupExecutionAdmissionPort>(),
                sp.GetRequiredService<LookupTrackMetadataHandler>()));
        services.TryAddScoped<ILookupTrackMetadataHandler>(sp =>
            sp.GetRequiredService<LookupTrackMetadataExecutionAdmissionDecorator>());
        services.TryAddScoped<LookupTrackMetadataListener>();
        return services;
    }
}
