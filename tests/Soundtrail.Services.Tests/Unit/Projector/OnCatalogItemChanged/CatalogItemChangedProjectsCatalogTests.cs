using Soundtrail.Domain.Catalog.Events;
namespace Soundtrail.Services.Tests.Unit.Projector.OnCatalogItemChanged;

public sealed class CatalogItemChangedProjectsCatalogTests
{
    [Fact]
    public async Task Given_A_Track_Is_Discovered_When_Projecting_Then_The_Artist_Catalog_Is_Updated()
    {
        var environment = CatalogItemChangedProjectorUnitTestEnvironment.Create();
        var subject = environment.CreateCatalogItemSubject();

        await subject.Handle(CatalogItemChangedProjectorUnitTestEnvironment.CreateTrackDiscovered());

        environment.Repository.AppendedEvents.Single().Should().BeOfType<TrackDiscovered>();
    }

    [Fact]
    public async Task Given_Playlist_Tracks_Are_Discovered_When_Projecting_Then_The_Playlist_Read_Model_Is_Stored()
    {
        var environment = CatalogItemChangedProjectorUnitTestEnvironment.Create();
        var subject = environment.CreatePlaylistSubject();
        var discovered = CatalogItemChangedProjectorUnitTestEnvironment.CreatePlaylistTracksDiscovered();

        await subject.Handle(discovered);

        environment.StorePlaylistTracksReadModelPort.StoredEvent.Should().Be(discovered);
        environment.CommandBus.SentMessages.Should().BeEmpty();
    }

    [Fact]
    public async Task Given_A_Streaming_Location_Is_Discovered_When_Projecting_Then_The_Artist_Catalog_Is_Updated()
    {
        var environment = CatalogItemChangedProjectorUnitTestEnvironment.Create();
        var subject = environment.CreateCatalogItemSubject();

        await subject.Handle(CatalogItemChangedProjectorUnitTestEnvironment.CreateStreamingLocationDiscovered());

        environment.Repository.AppendedEvents.Single().Should().BeOfType<StreamingLocationDiscovered>();
    }

    [Fact]
    public async Task Given_A_Catalog_Track_Change_When_Projecting_Then_Referencing_Playlists_Are_Repaired()
    {
        var environment = CatalogItemChangedProjectorUnitTestEnvironment.Create();
        var subject = environment.CreateCatalogTrackChangedSubject();
        var trackId = TestTrackIds.Create("projected-playlist-track");

        await subject.Handle(trackId);

        environment.StorePlaylistTracksReadModelPort.RepairedTrackId.Should().Be(trackId);
    }
}
