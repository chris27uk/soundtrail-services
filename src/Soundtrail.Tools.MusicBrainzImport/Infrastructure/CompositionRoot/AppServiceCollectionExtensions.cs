using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Soundtrail.Tools.MusicBrainzImport.Features.ImportMusicBrainzDump.CompositionRoot;
using Soundtrail.Tools.MusicBrainzImport.Features.RebuildAllReadModels.CompositionRoot;
using Soundtrail.Tools.MusicBrainzImport.Features.ReplayCatalogProjection.CompositionRoot;
using Soundtrail.Tools.MusicBrainzImport.Features.ReplayDiscoveryLifecycleProjection.CompositionRoot;
using Soundtrail.Tools.MusicBrainzImport.Features.ReplayPlannerMusicTrackProjection.CompositionRoot;
using Soundtrail.Tools.MusicBrainzImport.Infrastructure.Raven;
using Soundtrail.Translators.MusicTrackEventStore.CompositionRoot;

namespace Soundtrail.Tools.MusicBrainzImport.Infrastructure.CompositionRoot;

public static class AppServiceCollectionExtensions
{
    public static IServiceCollection AddMusicBrainzImportAppServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddMusicTrackStoredEventTranslations();
        services.AddMusicBrainzImportRaven(configuration);
        services.AddImportMusicBrainzDumpFeature();
        services.AddReplayPlannerMusicTrackProjectionFeature();
        services.AddReplayCatalogProjectionFeature();
        services.AddOnReplayCatalogSearchStatusFeature();
        services.AddRebuildAllReadModelsFeature();
        return services;
    }
}
