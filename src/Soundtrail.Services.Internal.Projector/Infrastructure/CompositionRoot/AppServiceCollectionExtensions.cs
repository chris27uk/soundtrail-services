using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchPlannedForLookup.CompositionRoot;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchStatusChanged.CompositionRoot;
using Soundtrail.Services.Internal.Projector.Features.OnMusicCatalogChanged.CompositionRoot;
using Soundtrail.Services.Internal.Projector.Features.OnKnownTrackRequested.CompositionRoot;
using Soundtrail.Services.Internal.Projector.Features.OnMusicTrackChanged.CompositionRoot;
using Soundtrail.Services.Internal.Projector.Features.OnMusicTrackSearchStarted.CompositionRoot;
using Soundtrail.Services.Internal.Projector.Infrastructure.Messaging;
using Soundtrail.Services.Internal.Projector.Infrastructure.Raven;
using Soundtrail.Translators.MusicTrackEventStore.CompositionRoot;

namespace Soundtrail.Services.Internal.Projector.Infrastructure.CompositionRoot;

public static class AppServiceCollectionExtensions
{
    public static IServiceCollection AddInternalProjectorAppServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddMusicTrackStoredEventTranslations();
        services.AddRavenDocumentStore(configuration);
        services.AddInternalProjectorServiceBus(configuration);
        services.AddOnCatalogSearchPlannedForLookupFeature();
        services.AddOnCatalogSearchStatusChangedFeature();
        services.AddOnKnownTrackRequestedFeature();
        services.AddOnMusicTrackSearchStartedFeature();
        services.AddOnMusicTrackChangedFeature();
        services.AddOnMusicCatalogChangedFeature();
        return services;
    }
}
