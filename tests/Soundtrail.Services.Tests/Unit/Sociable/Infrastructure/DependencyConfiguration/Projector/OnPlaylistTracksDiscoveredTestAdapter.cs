using Microsoft.Extensions.Configuration;
using Soundtrail.Adapters.Timing;
using Soundtrail.Services.Internal.Projector.Features.OnArtistCatalogChanged.Adapters;
using Soundtrail.Services.Internal.Projector.Features.OnPlaylistTracksDiscovered.Adapters;
using Soundtrail.Services.Internal.Projector.Features.OnPlaylistTracksDiscovered.Composition;
using Soundtrail.Services.Internal.Projector.Features.OnWorkFeedbackChanged.Adapters;
using Soundtrail.Services.Internal.Projector.Infrastructure;
using Soundtrail.Services.Tests.Unit.Sociable.Infrastructure.Fakes;

namespace Soundtrail.Services.Tests.Unit.Sociable.Infrastructure.DependencyConfiguration.Projector;

internal sealed class OnPlaylistTracksDiscoveredTestAdapter(OnPlaylistTracksDiscoveredPorts ports)
    : ISociableFeature, IProjectorFeature
{
    public static OnPlaylistTracksDiscoveredTestAdapter Default() => new(DefaultPorts());

    public static OnPlaylistTracksDiscoveredTestAdapter With(
        Func<OnPlaylistTracksDiscoveredPorts, OnPlaylistTracksDiscoveredPorts> customize) =>
        new(customize(DefaultPorts()));

    public static OnPlaylistTracksDiscoveredPorts DefaultPorts() =>
        new(
            sp => new StorePlaylistTracksReadModelPortFake(
                sp.GetRequiredService<IClockPort>(),
                TestPortResolution.RequireFake<IStoreArtistCatalogReadModelPort, StoreArtistCatalogReadModelPortFake>(sp),
                TestPortResolution.RequireFake<IStoreDiscoveryFeedbackPort, StoreDiscoveryFeedbackPortFake>(sp)));

    public void ConfigureServices(IServiceCollection services, IConfiguration configuration) =>
        OnPlaylistTracksDiscoveredComposition.Configure(services, ports);

    public void ConfigureApplication(WebApplication app)
    {
    }
}
