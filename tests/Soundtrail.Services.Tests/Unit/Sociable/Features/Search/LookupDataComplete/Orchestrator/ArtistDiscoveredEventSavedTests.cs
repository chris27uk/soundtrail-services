using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Catalog.Events;
using Soundtrail.Services.Api.Features.Catalog.Search.Contract;

namespace Soundtrail.Services.Tests.Unit.Sociable.Search.LookupDataComplete.Orchestrator;

public sealed class ArtistDiscoveredEventSavedTests
{
    [Fact]
    public async Task Then_The_Artist_Name_Is_Saved()
    {
        const string name = "Artist Event Name";
        var environment = ForCompletedArtist(name: name);

        await environment.ProjectOnChange(sut => sut.Handle(environment.CreateRequest()));

        environment.SavedEvent<ArtistDiscovered>().Artist.Name.Value.Should().Be(name);
    }

    [Fact]
    public async Task Then_The_Artist_Id_Is_Saved()
    {
        var artistId = ArtistId.From("artist-event-source");
        var environment = ForCompletedArtist(artistId: artistId);

        await environment.ProjectOnChange(sut => sut.Handle(environment.CreateRequest()));

        environment.SavedEvent<ArtistDiscovered>().Artist.Id.Should().Be(artistId);
    }

    [Fact]
    public async Task Then_The_Observed_Time_Comes_From_The_Request()
    {
        var requestTime = new DateTimeOffset(2026, 8, 2, 11, 1, 0, TimeSpan.Zero);
        var environment = ForCompletedArtist(requestTime: requestTime);

        await environment.ProjectOnChange(sut => sut.Handle(environment.CreateRequest()));

        environment.SavedEvent<ArtistDiscovered>().ObservedAt.Should().Be(requestTime);
    }

    private static SearchSociableTestEnvironment ForCompletedArtist(
        string name = "Scenario Artist",
        ArtistId? artistId = null,
        DateTimeOffset requestTime = default) =>
        SearchSociableTestEnvironment.ForLookupDataComplete(
            requestTime,
            LookupDataCompleteSearchArtist.Create(
                artistId ?? ArtistId.From("scenario-artist"),
                name));
}
