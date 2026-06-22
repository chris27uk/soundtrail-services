using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soundtrail.Services.Enrichment.Orchestrator.Features.ProjectMusicTrackProjection;
using Soundtrail.Services.Enrichment.Orchestrator.Features.ProjectMusicTrackProjection.Adapters;
using Soundtrail.Tools.MusicBrainzImport.Features.ReplayPlannerMusicTrackProjection.Adapters;

namespace Soundtrail.Tools.MusicBrainzImport.Features.ReplayPlannerMusicTrackProjection.CompositionRoot;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddReplayPlannerMusicTrackProjectionFeature(this IServiceCollection services)
    {
        services.TryAddScoped<ReplayPlannerMusicTrackProjectionBatchHandler>();
        services.TryAddScoped<ProjectMusicTrackProjectionHandler>();
        services.TryAddScoped<IResetPlannerMusicTrackProjectionPort, RavenResetPlannerMusicTrackProjection>();
        services.TryAddSingleton<RavenMusicTrackProjectionMapper>();
        return services;
    }
}
