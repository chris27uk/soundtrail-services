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
        services.TryAddScoped<LookupStreamingLocationsBudgetReservationDecorator>(sp =>
            new LookupStreamingLocationsBudgetReservationDecorator(
                sp.GetRequiredService<IReserveSourceApiBudgetPort>(),
                sp.GetRequiredService<LookupStreamingLocationsHandler>()));
        services.TryAddScoped<LookupStreamingLocationsIdempotencyDecorator>(sp =>
            new LookupStreamingLocationsIdempotencyDecorator(
                sp.GetRequiredService<ILookupExecutionReceiptStore>(),
                sp.GetRequiredService<LookupStreamingLocationsBudgetReservationDecorator>()));
        services.TryAddScoped<ILookupStreamingLocationsHandler>(sp =>
            sp.GetRequiredService<LookupStreamingLocationsIdempotencyDecorator>());
        services.TryAddScoped<LookupStreamingLocationsListener>();
        return services;
    }
}
