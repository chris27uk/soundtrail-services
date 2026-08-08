using Soundtrail.Domain.Discovery;
using Soundtrail.Services.Api.Features.Catalog.GetAlbumsForArtist.Contract;

namespace Soundtrail.Services.Tests.Unit.Sociable.GetAlbumsForArtist.NoAlbumsFound.Orchestrator;

public sealed class WorkCompletedEventSavedTests
{
    [Fact]
    public async Task Then_No_Streaming_Location_Requests_Are_Enqueued()
    {
        var environment = GetAlbumsForArtistSociableTestEnvironment.ForNoAlbumsFound();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetAlbumsForArtistRequest(environment.ArtistId)));

        environment.SentMessages<RequestKnownMusicDataMessage>()
            .Where(message => message.Operation is CatalogItemOperation.StreamingLocationForTrack)
            .Should().BeEmpty();
    }
}
