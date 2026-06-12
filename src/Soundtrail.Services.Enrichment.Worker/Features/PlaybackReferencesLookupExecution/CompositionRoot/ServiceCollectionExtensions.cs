using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soundtrail.Services.Enrichment.Worker.Features.PlaybackReferencesLookupExecution.Adapters;

namespace Soundtrail.Services.Enrichment.Worker.Features.PlaybackReferencesLookupExecution.CompositionRoot;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPlaybackReferencesLookupExecutionFeature(
        this IServiceCollection services,
        Action<PlaybackReferencesLookupExecutionFeatureOptions>? configure = null)
    {
        var options = new PlaybackReferencesLookupExecutionFeatureOptions();
        configure?.Invoke(options);
        options.ConfigureDependencies?.Invoke(services);

        services.TryAddScoped<ExecutePlaybackReferencesLookupHandler>();
        services.TryAddScoped<PlaybackReferencesLookupExecutionListener>();
        return services;
    }
}
