using Soundtrail.Domain.Catalog.Albums;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Events;

namespace Soundtrail.Services.Tests.Unit.Sociable.Features.Search.Scenarios.LocalAlbumCandidate.Orchestrator;

public sealed class WorkRequestedEventSavedTests
{
    [Fact]
    public async Task Given_A_Local_Album_Candidate_When_Requesting_Then_Album_Track_Discovery_Work_Is_Requested()
    {
        var albumId = AlbumId.From("artist-123", "album-123");
        var environment = SearchSociableTestEnvironment.ForLocalAlbumCandidate(albumId);

        await environment.ProjectOnChange(sut => sut.Handle(environment.CreateRequest()));

        environment.SavedEvents<WorkRequested>().Select(@event => @event.Target)
            .Should().ContainSingle()
            .Which.Should().Be(Work.DiscoverAlbumTracks(albumId));
    }
}
