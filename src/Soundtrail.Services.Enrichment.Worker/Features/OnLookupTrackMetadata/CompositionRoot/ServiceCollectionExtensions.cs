using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Enrichment.Commands;
using Soundtrail.Services.Enrichment.Worker.Features.OnLookupTrackMetadata.Adapters;
using Soundtrail.Services.Enrichment.Worker.Features.OnLookupTrackMetadata.Pipeline;
using Soundtrail.Services.Enrichment.Worker.Shared.ExecutionAdmission;

namespace Soundtrail.Services.Enrichment.Worker.Features.OnLookupTrackMetadata.CompositionRoot;

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
                sp.GetRequiredService<ILookupExecutionAdmissionPort>(),
                sp.GetRequiredService<LookupTrackMetadataHandler>(),
                sp.GetRequiredService<ICommandBus>()));
        services.TryAddScoped<IHandler<LookupTrackMetadataCommand>>(sp =>
            sp.GetRequiredService<LookupTrackMetadataExecutionAdmissionDecorator>());
        services.TryAddScoped<LookupTrackMetadataListener>();
        return services;
    }
}
