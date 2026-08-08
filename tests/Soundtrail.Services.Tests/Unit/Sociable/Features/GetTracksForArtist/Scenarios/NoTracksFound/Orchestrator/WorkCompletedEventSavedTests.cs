using Soundtrail.Domain.Discovery;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForArtist.Contract;

namespace Soundtrail.Services.Tests.Unit.Sociable.Features.GetTracksForArtist.Scenarios.NoTracksFound.Orchestrator;

public sealed class WorkCompletedEventSavedTests
{
    [Fact]
    public async Task Then_No_Streaming_Location_Requests_Are_Enqueued()
    {
        var environment = GetTracksForArtistSociableTestEnvironment.ForNoTracksFound();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForArtistRequest(environment.ArtistId)));

        environment.SentMessages<RequestKnownMusicDataMessage>()
            .Where(message => message.Operation is CatalogItemOperation.StreamingLocationForTrack)
            .Should().BeEmpty();
    }
}
