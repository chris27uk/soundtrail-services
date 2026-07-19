using Soundtrail.Domain.Catalog.Events;
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
    public async Task Given_A_Playlist_Lookup_Success_When_Handling_Then_A_Track_Is_Discovered()
    {
        var environment = LookupCompletedHandlerUnitTestEnvironment.Create();
        environment.SeedForPlaylist();
        var subject = environment.CreateSubject();

        await subject.Handle(LookupCompletedHandlerUnitTestEnvironment.CreatePlaylistCompleted());

        environment.Repository.AppendedEvents.First().Should().BeOfType<TrackDiscovered>();
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
}
