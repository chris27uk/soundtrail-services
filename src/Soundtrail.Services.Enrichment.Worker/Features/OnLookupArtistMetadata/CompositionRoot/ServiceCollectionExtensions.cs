using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Enrichment.Commands;
using Soundtrail.Services.Enrichment.Worker.Features.OnLookupArtistMetadata.Adapters;
using Soundtrail.Services.Enrichment.Worker.Features.OnLookupArtistMetadata.Pipeline;
using Soundtrail.Services.Enrichment.Worker.Shared.ExecutionAdmission;

namespace Soundtrail.Services.Enrichment.Worker.Features.OnLookupArtistMetadata.CompositionRoot;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddLookupArtistMetadataFeature(this IServiceCollection services)
    {
        services.TryAddScoped<LookupArtistMetadataHandler>();
        services.TryAddScoped<LookupArtistMetadataExecutionAdmissionDecorator>(sp =>
            new LookupArtistMetadataExecutionAdmissionDecorator(
                sp.GetRequiredService<ILookupExecutionAdmissionPort>(),
                sp.GetRequiredService<LookupArtistMetadataHandler>(),
                sp.GetRequiredService<ICommandBus>()));
        services.TryAddScoped<IHandler<LookupArtistMetadataCommand>>(sp =>
            sp.GetRequiredService<LookupArtistMetadataExecutionAdmissionDecorator>());
        services.TryAddScoped<LookupArtistMetadataListener>();
        return services;
    }
}
