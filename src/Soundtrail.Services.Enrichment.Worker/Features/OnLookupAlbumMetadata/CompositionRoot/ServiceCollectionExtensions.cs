using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Enrichment.Commands;
using Soundtrail.Services.Enrichment.Worker.Features.OnLookupAlbumMetadata.Adapters;
using Soundtrail.Services.Enrichment.Worker.Features.OnLookupAlbumMetadata.Pipeline;
using Soundtrail.Services.Enrichment.Worker.Shared.ExecutionAdmission;

namespace Soundtrail.Services.Enrichment.Worker.Features.OnLookupAlbumMetadata.CompositionRoot;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddLookupAlbumMetadataFeature(this IServiceCollection services)
    {
        services.TryAddScoped<LookupAlbumMetadataHandler>();
        services.TryAddScoped<LookupAlbumMetadataExecutionAdmissionDecorator>(sp =>
            new LookupAlbumMetadataExecutionAdmissionDecorator(
                sp.GetRequiredService<ILookupExecutionAdmissionPort>(),
                sp.GetRequiredService<LookupAlbumMetadataHandler>(),
                sp.GetRequiredService<ICommandBus>()));
        services.TryAddScoped<IHandler<LookupAlbumMetadataCommand>>(sp =>
            sp.GetRequiredService<LookupAlbumMetadataExecutionAdmissionDecorator>());
        services.TryAddScoped<LookupAlbumMetadataListener>();
        return services;
    }
}
