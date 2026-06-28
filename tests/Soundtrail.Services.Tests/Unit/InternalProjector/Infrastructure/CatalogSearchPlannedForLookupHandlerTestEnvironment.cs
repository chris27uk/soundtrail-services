using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Events;
using Soundtrail.Domain.Search;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchPlannedForLookup;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchPlannedForLookup.Ports;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchPlannedForLookup.Support;
using Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

namespace Soundtrail.Services.Tests.Unit.InternalProjector.Infrastructure;

internal sealed class CatalogSearchPlannedForLookupHandlerTestEnvironment
{
    private readonly LoadCatalogSearchPlannedMusicTrackPortFake loadMusicTrackPort;

    private CatalogSearchPlannedForLookupHandlerTestEnvironment()
    {
        loadMusicTrackPort = new LoadCatalogSearchPlannedMusicTrackPortFake();
        Bus = new CommandBusFake();
        Handler = new CatalogSearchPlannedForLookupHandler(
            loadMusicTrackPort,
            Bus);
    }

    public CommandBusFake Bus { get; }

    public CatalogSearchPlannedForLookupHandler Handler { get; }

    public static CatalogSearchPlannedForLookupHandlerTestEnvironment Create() => new();

    public void TrackIsMissing() => loadMusicTrackPort.Return(null);

    public void TrackRequiresMetadata() => loadMusicTrackPort.Return(
        new CatalogSearchPlannedMusicTrack(null, null, false, null, null, null, null, null, null, null));

    public CatalogSearchPlannedForLookupCommand Command(MusicSearchCriteria searchCriteria, MusicCatalogId musicCatalogId) =>
        new(
            searchCriteria,
            [
                new VersionedCatalogSearchDiscoveryEvent(
                    1,
                    new CatalogCandidateIdentified(
                        searchCriteria,
                        musicCatalogId,
                        1,
                        10,
                        Clock.AddSeconds(-5),
                        CorrelationId.From("corr-1"))),
                new VersionedCatalogSearchDiscoveryEvent(
                    2,
                    new DiscoveryPlanned(
                        searchCriteria,
                        LookupPriorityBand.High,
                        true,
                        30,
                        null,
                        "Planner queued lookup",
                        Clock))
            ]);

    private sealed class LoadCatalogSearchPlannedMusicTrackPortFake : ILoadCatalogSearchPlannedMusicTrackPort
    {
        private CatalogSearchPlannedMusicTrack? track;

        public void Return(CatalogSearchPlannedMusicTrack? value) => track = value;

        public Task<CatalogSearchPlannedMusicTrack?> LoadAsync(MusicCatalogId musicCatalogId, CancellationToken cancellationToken)
        {
            _ = musicCatalogId;
            _ = cancellationToken;
            return Task.FromResult(track);
        }
    }

    private static readonly DateTimeOffset Clock = new(2026, 6, 28, 12, 0, 0, TimeSpan.Zero);
}
