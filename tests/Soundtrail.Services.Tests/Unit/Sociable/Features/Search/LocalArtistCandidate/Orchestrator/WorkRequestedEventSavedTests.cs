using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Events;
using Soundtrail.Services.Api.Features.Catalog.Search.Contract;

namespace Soundtrail.Services.Tests.Unit.Sociable.Search.LocalArtistCandidate.Orchestrator;

public sealed class WorkRequestedEventSavedTests
{
    [Fact]
    public async Task Given_A_Local_Artist_Candidate_When_Requesting_Then_Album_And_Track_Discovery_Work_Are_Requested()
    {
        var artistId = ArtistId.From("artist-123");
        var environment = SearchSociableTestEnvironment.ForLocalArtistCandidate(artistId);

        await environment.ProjectOnChange(sut => sut.Handle(environment.CreateRequest()));

        environment.SavedEvents<WorkRequested>().Select(@event => @event.Target).Should().BeEquivalentTo([
            Work.DiscoverArtistAlbums(artistId),
            Work.DiscoverArtistTracks(artistId)
        ]);
    }
}
