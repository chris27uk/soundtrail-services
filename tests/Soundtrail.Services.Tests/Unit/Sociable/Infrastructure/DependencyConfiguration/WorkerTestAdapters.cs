using Microsoft.Extensions.Configuration;
using Soundtrail.Adapters.FeatureOrchestration;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Services.Enrichment.Worker.Features.LookupPlaylistTracks.Ports;
using Soundtrail.Services.Enrichment.Worker.Shared.Composition;
using Soundtrail.Services.Tests.Fakes;
using Soundtrail.Services.Tests.Unit.Sociable.GetTracksForPlaylist.Composition;
using Soundtrail.Services.Tests.Unit.Sociable.Infrastructure.Fakes;

namespace Soundtrail.Services.Tests.Unit.Sociable.Infrastructure.DependencyConfiguration;

internal sealed class WorkerTestAdapters(CatalogEnrichmentReadPorts ports) : IFeature
{
    public static WorkerTestAdapters Default() =>
        new(CreateDefaultPorts());

    public static WorkerTestAdapters WithFailingPlaylistTracksRead(Exception error) =>
        new(CreateDefaultPorts(readPlaylistTracks: _ => ReadPlaylistTracksByProviderPortFake.ThatThrows(error)));

    public void ConfigureServices(IServiceCollection services, IConfiguration configuration) =>
        CatalogEnrichmentReadComposition.Configure(services, ports);

    private static CatalogEnrichmentReadPorts CreateDefaultPorts(
        Func<IServiceProvider, IReadPlaylistTracksByProviderPort>? readPlaylistTracks = null) =>
        new(
            readPlaylistTracks ?? (_ => ReadPlaylistTracksByProviderPortFake.Empty()),
            _ => new ReadCatalogEntriesBySearchCriteriaPortFake(),
            _ => new ReadStreamingLocationByProviderPortFake(),
            _ => new ReadTrackForLookupPortFake(),
            _ => new ReadAlbumsByArtistIdPortFake(),
            _ => new ReadTracksByArtistIdPortFake(),
            _ => new ReadTracksByAlbumIdPortFake(),
            sp => new ClockFake(sp.GetRequiredService<SociableScenarioOptions>().UtcNow),
            sp => sp.GetRequiredService<ICommandBus>());
}
