using Soundtrail.Domain.Catalog.Events;
using Soundtrail.Domain.Operations;

namespace Soundtrail.Services.Tests.Unit.Projector.OnLookupRecorded;

public sealed class DiscoveryOutcomeProjectsCatalogTests
{
    [Fact]
    public async Task Given_A_Track_Is_Discovered_When_Projecting_Then_The_Artist_Catalog_Is_Updated()
    {
        var environment = DiscoveryOutcomeProjectorUnitTestEnvironment.Create();
        var subject = environment.CreateDiscoverySubject();

        await subject.Handle(DiscoveryOutcomeProjectorUnitTestEnvironment.CreateTrackDiscovered());

        environment.Repository.AppendedEvents.Single().Should().BeOfType<TrackDiscovered>();
    }

    [Fact]
    public async Task Given_Playlist_Tracks_Are_Discovered_When_Projecting_Then_A_Playlist_Update_Is_Sent()
    {
        var environment = DiscoveryOutcomeProjectorUnitTestEnvironment.Create();
        var subject = environment.CreateDiscoverySubject();

        await subject.Handle(DiscoveryOutcomeProjectorUnitTestEnvironment.CreatePlaylistTracksDiscovered());

        environment.CommandBus.Commands.Single().Should().BeOfType<PlaylistUpdated>();
    }

    [Fact]
    public async Task Given_A_Streaming_Location_Is_Discovered_When_Projecting_Then_The_Artist_Catalog_Is_Updated()
    {
        var environment = DiscoveryOutcomeProjectorUnitTestEnvironment.Create();
        var subject = environment.CreateStreamingLocationSubject();

        await subject.Handle(DiscoveryOutcomeProjectorUnitTestEnvironment.CreateStreamingLocationDiscovered());

        environment.Repository.AppendedEvents.Single().Should().BeOfType<StreamingLocationDiscovered>();
    }
}
