using Microsoft.Extensions.Configuration;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Services.Enrichment.Worker.Shared.StreamingLocations;
using Soundtrail.Services.Internal.Projector.Features.OnArtistCatalogChanged.Adapters;
using Soundtrail.Services.Internal.Projector.Features.OnArtistCatalogChanged.Composition;
using Soundtrail.Services.Internal.Projector.Infrastructure;
using Soundtrail.Services.Tests.Unit.Sociable.Infrastructure.Fakes;

namespace Soundtrail.Services.Tests.Unit.Sociable.Infrastructure.DependencyConfiguration.Projector;

internal sealed class OnArtistCatalogChangedTestAdapter(OnArtistCatalogChangedPorts ports)
    : ISociableFeature, IProjectorFeature
{
    public static OnArtistCatalogChangedTestAdapter Default() => new(DefaultPorts());

    public static OnArtistCatalogChangedTestAdapter With(
        Func<OnArtistCatalogChangedPorts, OnArtistCatalogChangedPorts> customize) =>
        new(customize(DefaultPorts()));

    public static OnArtistCatalogChangedPorts DefaultPorts() =>
        new(
            sp => new StoreArtistCatalogReadModelPortFake(
                TestPortResolution.RequireFake<IReadTrackForLookupPort, ReadTrackForLookupPortFake>(sp)),
            _ => new InMemoryEventStreamRepository<ArtistId>());

    public void ConfigureServices(IServiceCollection services, IConfiguration configuration) =>
        OnArtistCatalogChangedComposition.Configure(services, ports);

    public void ConfigureApplication(WebApplication app)
    {
    }
}
