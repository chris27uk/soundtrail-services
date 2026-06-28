using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soundtrail.Services.Internal.Projector.Features.OnMusicTrackChanged;
using Soundtrail.Services.Internal.Projector.Features.OnMusicTrackChanged.Adapters;
using Soundtrail.Tools.MusicBrainzImport.Features.ReplayPlannerMusicTrackProjection.Adapters;
using Soundtrail.Tools.MusicBrainzImport.Features.ReplayPlannerMusicTrackProjection.ProjectionReset;
using Soundtrail.Adapters.ProjectionDocuments;

namespace Soundtrail.Tools.MusicBrainzImport.Features.ReplayPlannerMusicTrackProjection.CompositionRoot;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddReplayPlannerMusicTrackProjectionFeature(this IServiceCollection services)
    {
        services.TryAddScoped<ReplayPlannerMusicTrackProjectionBatchHandler>();
        services.TryAddScoped<MusicTrackChangedHandler>();
        services.TryAddScoped<IResetPlannerMusicTrackProjectionPort, RavenResetPlannerMusicTrackProjection>();
        services.TryAddSingleton<RavenMusicTrackProjectionMapper>();
        return services;
    }
}
