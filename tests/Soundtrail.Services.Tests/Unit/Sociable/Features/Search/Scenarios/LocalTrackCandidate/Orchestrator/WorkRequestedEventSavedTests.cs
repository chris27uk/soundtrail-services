using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Events;

namespace Soundtrail.Services.Tests.Unit.Sociable.Features.Search.Scenarios.LocalTrackCandidate.Orchestrator;

public sealed class WorkRequestedEventSavedTests
{
    [Fact]
    public async Task Given_A_Local_Track_Candidate_When_Requesting_Then_Track_Streaming_Location_Work_Is_Requested()
    {
        var trackId = TrackId.From(TestTrackIds.Value("track-123"));
        var environment = SearchSociableTestEnvironment.ForLocalTrackCandidate(trackId);

        await environment.ProjectOnChange(sut => sut.Handle(environment.CreateRequest()));

        environment.SavedEvents<WorkRequested>().Select(@event => @event.Target)
            .Should().ContainSingle()
            .Which.Should().Be(Work.EnrichTrackStreamingLocation(trackId));
    }
}
