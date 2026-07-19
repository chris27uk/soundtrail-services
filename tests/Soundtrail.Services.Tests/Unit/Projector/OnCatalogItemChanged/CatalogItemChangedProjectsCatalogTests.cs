using Soundtrail.Domain.Catalog.Events;
using Soundtrail.Domain.Operations;

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
    public async Task Given_Playlist_Tracks_Are_Discovered_When_Projecting_Then_A_Playlist_Update_Is_Sent()
    {
        var environment = CatalogItemChangedProjectorUnitTestEnvironment.Create();
        var subject = environment.CreatePlaylistSubject();

        await subject.Handle(CatalogItemChangedProjectorUnitTestEnvironment.CreatePlaylistTracksDiscovered());

        environment.CommandBus.Commands.Single().Should().BeOfType<PlaylistUpdated>();
    }

    [Fact]
    public async Task Given_A_Streaming_Location_Is_Discovered_When_Projecting_Then_The_Artist_Catalog_Is_Updated()
    {
        var environment = CatalogItemChangedProjectorUnitTestEnvironment.Create();
        var subject = environment.CreateCatalogItemSubject();

        await subject.Handle(CatalogItemChangedProjectorUnitTestEnvironment.CreateStreamingLocationDiscovered());

        environment.Repository.AppendedEvents.Single().Should().BeOfType<StreamingLocationDiscovered>();
    }
}
