using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soundtrail.Services.Enrichment.Worker.Features.OnLookupMusicMetadata.Adapters;
using Soundtrail.Services.Enrichment.Worker.Features.OnLookupMusicMetadata.Pipeline;
using Soundtrail.Services.Enrichment.Worker.Infrastructure.Idempotency.Storage;
using Soundtrail.Domain.Discovery;

namespace Soundtrail.Services.Enrichment.Worker.Features.OnLookupMusicMetadata.CompositionRoot;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddLookupMusicMetadataFeature(
        this IServiceCollection services,
        Action<LookupMusicMetadataFeatureOptions>? configure = null)
    {
        var options = new LookupMusicMetadataFeatureOptions();
        configure?.Invoke(options);
        options.ConfigureDependencies?.Invoke(services);

        services.TryAddScoped<LookupMusicMetadataHandler>();
        services.TryAddScoped<LookupMusicMetadataBudgetReservationDecorator>(sp =>
            new LookupMusicMetadataBudgetReservationDecorator(
                sp.GetRequiredService<IReserveSourceApiBudgetPort>(),
                sp.GetRequiredService<LookupMusicMetadataHandler>()));
        services.TryAddScoped<LookupMusicMetadataIdempotencyDecorator>(sp =>
            new LookupMusicMetadataIdempotencyDecorator(
                sp.GetRequiredService<ILookupExecutionReceiptStore>(),
                sp.GetRequiredService<LookupMusicMetadataBudgetReservationDecorator>()));
        services.TryAddScoped<ILookupMusicMetadataHandler>(sp =>
            sp.GetRequiredService<LookupMusicMetadataIdempotencyDecorator>());
        services.TryAddScoped<LookupMusicMetadataListener>();
        return services;
    }
}
