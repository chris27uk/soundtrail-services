using Soundtrail.Domain.Discovery;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForAlbum.Contract;

namespace Soundtrail.Services.Tests.Unit.Sociable.Features.GetTracksForAlbum.Scenarios.NoTracksFound.Orchestrator;

public sealed class WorkCompletedEventSavedTests
{
    [Fact]
    public async Task Then_No_Streaming_Location_Requests_Are_Enqueued()
    {
        var environment = GetTracksForAlbumSociableTestEnvironment.ForNoTracksFound();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForAlbumRequest(environment.AlbumId)));

        environment.SentMessages<RequestKnownMusicDataMessage>()
            .Where(message => message.Operation is CatalogItemOperation.StreamingLocationForTrack)
            .Should().BeEmpty();
    }
}
