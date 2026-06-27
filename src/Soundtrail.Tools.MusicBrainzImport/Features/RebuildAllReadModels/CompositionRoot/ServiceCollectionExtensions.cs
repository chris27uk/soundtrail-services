using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soundtrail.Tools.MusicBrainzImport.Features.RebuildAllReadModels.Adapters;
using Soundtrail.Tools.MusicBrainzImport.Features.RebuildAllReadModels.OperationalState;

namespace Soundtrail.Tools.MusicBrainzImport.Features.RebuildAllReadModels.CompositionRoot;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRebuildAllReadModelsFeature(this IServiceCollection services)
    {
        services.TryAddScoped<RebuildAllReadModelsHandler>();
        services.TryAddScoped<IClearPlannerOperationalStatePort, RavenClearPlannerOperationalState>();
        return services;
    }
}
