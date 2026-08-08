using Soundtrail.Domain.Discovery;
using Soundtrail.Services.Api.Features.Catalog.Search.Contract;

namespace Soundtrail.Services.Tests.Unit.Sociable.Search.NoResultsFound.Orchestrator;

public sealed class WorkCompletedEventSavedTests
{
    [Fact]
    public async Task Then_No_Streaming_Location_Requests_Are_Enqueued()
    {
        var environment = SearchSociableTestEnvironment.ForNoResultsFound();

        await environment.ProjectOnChange(sut => sut.Handle(environment.CreateRequest()));

        environment.SentMessages<RequestKnownMusicDataMessage>()
            .Where(message => message.Operation is CatalogItemOperation.StreamingLocationForTrack)
            .Should().BeEmpty();
    }
}
