using Soundtrail.Domain.Discovery.Events;
using Soundtrail.Services.Api.Features.Catalog.Search.Contract;

namespace Soundtrail.Services.Tests.Unit.Sociable.Search.LocalPlaylistCandidate.Orchestrator;

public sealed class WorkRequestedEventSavedTests
{
    [Fact]
    public async Task Given_A_Local_Playlist_Candidate_When_Requesting_Then_No_Work_Is_Requested()
    {
        var environment = SearchSociableTestEnvironment.ForLocalPlaylistCandidate();

        await environment.ProjectOnChange(sut => sut.Handle(environment.CreateRequest()));

        environment.SavedEvents<WorkRequested>().Should().BeEmpty();
    }
}
