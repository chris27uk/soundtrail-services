using Microsoft.Extensions.Configuration;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Services.Enrichment.Worker.Shared.Composition;
using Soundtrail.Services.Tests.Unit.Sociable.Infrastructure.Fakes;
using Soundtrail.Services.Tests.Unit.Sociable.GetTracksForPlaylist.Composition;
using Soundtrail.Services.Tests.Unit.Sociable.Features.GetTracksForPlaylist;
using Soundtrail.Services.Tests.Unit.Sociable.Features.Search;
using Soundtrail.Services.Tests.Unit.Sociable.Features.GetTracksForArtist;
using Soundtrail.Services.Tests.Unit.Sociable.Features.GetTracksForAlbum;
using Soundtrail.Services.Tests.Unit.Sociable.Features.GetAlbumsForArtist;

namespace Soundtrail.Services.Tests.Unit.Sociable.Infrastructure.DependencyConfiguration.Worker;

internal sealed class CatalogEnrichmentReadTestAdapter(CatalogEnrichmentReadPorts ports) : ISociableFeature
{
    public static CatalogEnrichmentReadTestAdapter Default() => new(DefaultPorts());

    public static CatalogEnrichmentReadTestAdapter With(
        Func<CatalogEnrichmentReadPorts, CatalogEnrichmentReadPorts> customize) =>
        new(customize(DefaultPorts()));

    public static CatalogEnrichmentReadPorts DefaultPorts() =>
        new(
            _ => ReadPlaylistTracksByProviderPortFake.Empty(),
            _ => new ReadCatalogEntriesBySearchCriteriaPortFake(),
            _ => new ReadStreamingLocationByProviderPortFake(),
            _ => new ReadTrackForLookupPortFake(),
            _ => new ReadAlbumsByArtistIdPortFake(),
            _ => new ReadTracksByArtistIdPortFake(),
            _ => new ReadTracksByAlbumIdPortFake(),
            sp => new ClockFake(sp.GetRequiredService<SociableScenarioOptions>().UtcNow),
            sp => sp.GetRequiredService<ICommandBus>());

    public void ConfigureServices(IServiceCollection services, IConfiguration configuration) =>
        CatalogEnrichmentReadComposition.Configure(services, ports);
}
