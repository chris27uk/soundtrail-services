using Microsoft.Extensions.Configuration;
using Soundtrail.Adapters.FeatureOrchestration;
using Soundtrail.Adapters.Timing;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Services.Enrichment.Worker.Shared.StreamingLocations;
using Soundtrail.Services.Internal.Projector.Features.OnArtistCatalogChanged.Adapters;
using Soundtrail.Services.Internal.Projector.Features.OnArtistCatalogChanged.Composition;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchCandidateChanged.Composition;
using Soundtrail.Services.Internal.Projector.Features.OnPlaylistTracksDiscovered.Composition;
using Soundtrail.Services.Internal.Projector.Features.OnWorkFeedbackChanged.Adapters;
using Soundtrail.Services.Internal.Projector.Features.OnWorkFeedbackChanged.Composition;
using Soundtrail.Services.Internal.Projector.Infrastructure;
using Soundtrail.Services.Tests.Fakes;
using Soundtrail.Services.Tests.Unit.Sociable.GetTracksForPlaylist;
using Soundtrail.Services.Tests.Unit.Sociable.Infrastructure.Fakes;

namespace Soundtrail.Services.Tests.Unit.Sociable.Infrastructure.DependencyConfiguration;

internal sealed class ProjectorTestAdapters : IProjectorFeature
{
    public static ProjectorTestAdapters Default() => new();

    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        OnWorkFeedbackChangedComposition.Configure(services, new(
            _ => new StoreDiscoveryFeedbackPortFake()));

        OnCatalogSearchCandidateChangedComposition.Configure(services, new(
            _ => new StoreCatalogSearchCandidatePortFake()));

        OnArtistCatalogChangedComposition.Configure(services, new(
            sp => new StoreArtistCatalogReadModelPortFake(
                TestPortResolution.RequireFake<IReadTrackForLookupPort, ReadTrackForLookupPortFake>(sp)),
            _ => new InMemoryEventStreamRepository<ArtistId>()));

        OnPlaylistTracksDiscoveredComposition.Configure(services, new(
            sp => new StorePlaylistTracksReadModelPortFake(
                sp.GetRequiredService<IClockPort>(),
                TestPortResolution.RequireFake<IStoreArtistCatalogReadModelPort, StoreArtistCatalogReadModelPortFake>(sp),
                TestPortResolution.RequireFake<IStoreDiscoveryFeedbackPort, StoreDiscoveryFeedbackPortFake>(sp))));
    }

    public void ConfigureApplication(WebApplication app)
    {
    }
}
