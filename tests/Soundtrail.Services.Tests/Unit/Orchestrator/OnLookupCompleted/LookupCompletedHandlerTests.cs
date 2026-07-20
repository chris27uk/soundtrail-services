using Soundtrail.Domain.Catalog.Events;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Events;

namespace Soundtrail.Services.Tests.Unit.Orchestrator.OnLookupCompleted;

public sealed class LookupCompletedHandlerTests
{
    [Fact]
    public async Task Given_A_Streaming_Lookup_Success_When_Handling_Then_A_Streaming_Location_Is_Discovered()
    {
        var environment = LookupCompletedHandlerUnitTestEnvironment.Create();
        environment.SeedForStreamingLocation();
        var subject = environment.CreateSubject();

        await subject.Handle(LookupCompletedHandlerUnitTestEnvironment.CreateStreamingLocationCompleted());

        environment.Repository.AppendedEvents.First().Should().BeOfType<StreamingLocationDiscovered>();
    }

    [Fact]
    public async Task Given_A_Streaming_Lookup_Success_When_Handling_Then_Work_Is_Marked_Completed()
    {
        var environment = LookupCompletedHandlerUnitTestEnvironment.Create();
        environment.SeedForStreamingLocation();
        var subject = environment.CreateSubject();

        await subject.Handle(LookupCompletedHandlerUnitTestEnvironment.CreateStreamingLocationCompleted());

        environment.Repository.AppendedEvents.Last().Should().BeOfType<WorkCompleted>();
    }

    [Fact]
    public async Task Given_A_Playlist_Lookup_Success_When_Handling_Then_Playlist_Tracks_Are_Discovered()
    {
        var environment = LookupCompletedHandlerUnitTestEnvironment.Create();
        environment.SeedForPlaylist();
        var subject = environment.CreateSubject();

        await subject.Handle(LookupCompletedHandlerUnitTestEnvironment.CreatePlaylistCompleted());

        environment.Repository.AppendedEvents.First().Should().BeOfType<PlaylistTracksDiscovered>();
    }

    [Fact]
    public async Task Given_A_Deferred_Lookup_Result_When_Handling_Then_Work_Is_Marked_Deferred()
    {
        var environment = LookupCompletedHandlerUnitTestEnvironment.Create();
        var trackId = TestTrackIds.Create("lookup-deferred-1");
        environment.SeedForStreamingLocation(trackId);
        var subject = environment.CreateSubject();

        await subject.Handle(LookupCompletedHandlerUnitTestEnvironment.CreateDeferred());

        environment.Repository.AppendedEvents.Last().Should().BeOfType<WorkDeferred>();
    }

    [Fact]
    public async Task Given_A_Lookup_Result_When_Handling_Then_Stream_Is_Loaded_Once()
    {
        var environment = LookupCompletedHandlerUnitTestEnvironment.Create();
        environment.SeedForStreamingLocation();
        var subject = environment.CreateSubject();

        await subject.Handle(LookupCompletedHandlerUnitTestEnvironment.CreateStreamingLocationCompleted());

        environment.Repository.LoadCalls.Should().Be(1);
    }

    [Fact]
    public async Task Given_Multiple_Scheduled_Lookups_When_Handling_Then_The_Matching_Scheduled_Work_Is_Completed()
    {
        var environment = LookupCompletedHandlerUnitTestEnvironment.Create();
        var firstTrackId = TestTrackIds.Create("lookup-streaming-first");
        var secondTrackId = TestTrackIds.Create("lookup-streaming-second");
        environment.SeedWithMultipleScheduledStreamingLookups(firstTrackId, secondTrackId);
        var subject = environment.CreateSubject();
        var completed = LookupCompletedHandlerUnitTestEnvironment.CreateStreamingLocationCompleted(
            trackId: firstTrackId,
            originalCommandId: LookupCompletedHandlerUnitTestEnvironment.CreateWorkerCommandIdForScheduledWork(
                Work.EnrichTrackStreamingLocation(firstTrackId),
                new DateTimeOffset(2026, 7, 19, 9, 40, 30, TimeSpan.Zero),
                "streaming-isrc:Spotify"));

        await subject.Handle(completed);

        environment.Repository.AppendedEvents.Last().Should().Be(
            new WorkCompleted(
                Work.EnrichTrackStreamingLocation(firstTrackId),
                LookupPriorityBand.Low,
                "Lookup completed.",
                new DateTimeOffset(2026, 7, 19, 10, 0, 0, TimeSpan.Zero)));
    }
}
