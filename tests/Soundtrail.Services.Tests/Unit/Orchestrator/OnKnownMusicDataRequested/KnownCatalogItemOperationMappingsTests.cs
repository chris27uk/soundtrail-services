using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Events;

namespace Soundtrail.Services.Tests.Unit.Orchestrator.OnKnownMusicDataRequested;

public sealed class KnownCatalogItemOperationMappingsTests
{
    [Fact]
    public async Task Given_A_Known_Track_Request_When_Handling_Then_Track_Streaming_Location_Work_Is_Requested()
    {
        var environment = OnKnownMusicDataRequestedHandlerUnitTestEnvironment.Create();
        var subject = environment.CreateSubject();

        await subject.Handle(OnKnownMusicDataRequestedHandlerUnitTestEnvironment.CreateKnownTrackRequest(trackId: TestTrackIds.Value("track-123")));

        environment.Repository.AppendedEvents.OfType<WorkRequested>().Single().Target
            .Should().Be(Work.EnrichTrackStreamingLocation(TestTrackIds.Create("track-123")));
    }
}
