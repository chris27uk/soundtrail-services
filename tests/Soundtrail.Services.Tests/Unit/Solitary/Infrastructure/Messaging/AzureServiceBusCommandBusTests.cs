using Soundtrail.Adapters.Messaging.Asb;
using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.IntegrationMessaging.Commands;

namespace Soundtrail.Services.Tests.Unit.Solitary.Infrastructure.Messaging;

public sealed class AzureServiceBusCommandBusTests
{
    [Theory]
    [InlineData("search")]
    [InlineData("child_albums_for_artist")]
    [InlineData("child_tracks_for_artist")]
    [InlineData("child_tracks_for_album")]
    [InlineData("streaming_location_for_track")]
    [InlineData("child_tracks_for_playlist")]
    public void Given_A_Dispatch_Lookup_Work_Dto_When_Getting_The_Queue_Name_Then_It_Always_Maps_To_The_Dispatch_Queue(
        string targetKind)
    {
        var dto = new DispatchLookupWorkCommandDto(
            "cmd-123",
            "corr-123",
            new DateTimeOffset(2026, 7, 28, 9, 0, 0, TimeSpan.Zero),
            LookupPriorityBandDto.High,
            targetKind,
            "target-123",
            "playlist",
            0);

        var queueName = AzureServiceBusCommandBus.GetQueueName(dto);

        queueName.Should().Be("dispatch-lookup-work");
    }

    [Fact]
    public void Given_A_Playlist_Tracks_Lookup_Dto_When_Getting_The_Queue_Name_Then_It_Maps_To_The_Playlist_Worker_Queue()
    {
        var dto = new PlaylistTracksLookupCommandDto(
            "cmd-123",
            "corr-123",
            new DateTimeOffset(2026, 7, 28, 9, 0, 0, TimeSpan.Zero),
            LookupPriorityBandDto.High,
            "worldtop100",
            "Spotify");

        var queueName = AzureServiceBusCommandBus.GetQueueName(dto);

        queueName.Should().Be("lookup-music-playlists");
    }
}
