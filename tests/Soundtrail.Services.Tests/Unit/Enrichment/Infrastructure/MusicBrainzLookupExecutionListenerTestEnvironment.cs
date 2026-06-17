using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Model;
using Soundtrail.Domain.Responses;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.JustInTimeScheduling.Adapters;
using Soundtrail.Services.Enrichment.Worker.Infrastructure.Messaging;
using Soundtrail.Services.Enrichment.Worker.Features.OnDemandMetadataLookup;
using Soundtrail.Services.Enrichment.Worker.Features.OnDemandMetadataLookup.Adapters;

namespace Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

internal sealed class MusicBrainzLookupExecutionListenerTestEnvironment
{
    private static readonly LookupCanonicalMusicMetadataCommandDto DefaultCommand =
        new(
            CommandId.For("LookupCanonicalMusicMetadata:mc_track_1").Value,
            "mc_track_1",
            LookupPriorityBand.High,
            new DateTimeOffset(2026, 6, 8, 12, 0, 0, TimeSpan.Zero),
            "corr-1",
            "isrc-1",
            null,
            null,
            null,
            null,
            null);

    private MusicBrainzLookupExecutionListenerTestEnvironment(LookupExecutionReceiptStoreFake.State state)
    {
        Metadata = new FakeGetCanonicalMusicMetadata();
        DiscoveryRepository = new CatalogSearchDiscoveryRepositoryFake();
        var catalogSearchTrackings = new CatalogSearchTrackingStoreFake();
        catalogSearchTrackings.Seed(new CatalogSearchTracking(
            CatalogSearchCriteria.Search("track", "rare unknown song"),
            MusicCatalogId.From("mc_track_1"),
            new DateTimeOffset(2026, 6, 8, 12, 0, 0, TimeSpan.Zero)));
        Listener = new MusicBrainzLookupExecutionListener(
            new OnDemandLookupMetadataHandler(
                new LookupExecutionReceiptStoreFake(state),
                Metadata),
            catalogSearchTrackings,
            DiscoveryRepository);
    }

    public MusicBrainzLookupExecutionListener Listener { get; }

    public FakeGetCanonicalMusicMetadata Metadata { get; }

    public CatalogSearchDiscoveryRepositoryFake DiscoveryRepository { get; }

    public static MusicBrainzLookupExecutionListenerTestEnvironment WithANewExecutionCommandDto() =>
        new(new LookupExecutionReceiptStoreFake.State());

    public static MusicBrainzLookupExecutionListenerTestEnvironment WithADuplicateExecutionCommandDto() =>
        new(new LookupExecutionReceiptStoreFake.State());

    public void SeedMusicBrainzIsrc(string isrc, SongMetadata metadata) => Metadata.SeedIsrc(isrc, metadata);

    public void SeedMusicBrainzNames(string title, string artist, string? albumName, SongMetadata metadata) =>
        Metadata.SeedNames(title, artist, albumName, metadata);

    public void Throw(Exception ex) => Metadata.Throw(ex);

    public Task<object[]> HandleNewExecutionCommand(MusicSearchTerm? searchTerm = null) =>
        Listener.Handle(ToDto(searchTerm ?? MusicSearchTerm.ByIsrc("isrc-1")), null!);

    public async Task<object[]> HandleDuplicateExecutionCommand(MusicSearchTerm? searchTerm = null)
    {
        var dto = ToDto(searchTerm ?? MusicSearchTerm.ByIsrc("isrc-1"));
        await Listener.Handle(dto, null!);
        return await Listener.Handle(dto, null!);
    }

    private static LookupCanonicalMusicMetadataCommandDto ToDto(MusicSearchTerm searchTerm) =>
        searchTerm.Match((track, artist, album) =>
                DefaultCommand with
                {
                    Isrc = null,
                    TrackName = track,
                    ArtistName = artist,
                    AlbumName = album
                },
            isrc => DefaultCommand with
            {
                Isrc = isrc,
                TrackName = null,
                ArtistName = null,
                AlbumName = null
            });
}
