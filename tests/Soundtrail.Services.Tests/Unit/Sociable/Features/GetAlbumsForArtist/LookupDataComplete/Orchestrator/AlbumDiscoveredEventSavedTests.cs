using Soundtrail.Domain.Catalog.Albums;
using Soundtrail.Domain.Catalog.Events;
using Soundtrail.Services.Api.Features.Catalog.GetAlbumsForArtist.Contract;

namespace Soundtrail.Services.Tests.Unit.Sociable.GetAlbumsForArtist.LookupDataComplete.Orchestrator;

public sealed class AlbumDiscoveredEventSavedTests
{
    [Fact]
    public async Task Then_The_Album_Title_Is_Saved()
    {
        const string title = "Album Event Title";
        var environment = ForCompletedArtistAlbum(title: title);

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetAlbumsForArtistRequest(environment.ArtistId)));

        environment.SavedEvent<AlbumDiscovered>().Album.AlbumTitle.Should().Be(title);
    }

    [Fact]
    public async Task Then_The_Album_Id_Is_Saved()
    {
        const string title = "Album Event Title";
        var expected = AlbumId.From(
            LookupDataCompleteArtistAlbumScenarios.DefaultArtistId.Value,
            "album-event-source");
        var environment = ForCompletedArtistAlbum(title: title, sourceAlbumId: "album-event-source");

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetAlbumsForArtistRequest(environment.ArtistId)));

        environment.SavedEvent<AlbumDiscovered>().Album.AlbumId.Should().Be(expected);
    }

    [Fact]
    public async Task Then_The_Observed_Time_Comes_From_The_Request()
    {
        var requestTime = new DateTimeOffset(2026, 8, 2, 11, 1, 0, TimeSpan.Zero);
        var environment = ForCompletedArtistAlbum(requestTime: requestTime);

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetAlbumsForArtistRequest(environment.ArtistId)));

        environment.SavedEvent<AlbumDiscovered>().ObservedAt.Should().Be(requestTime);
    }

    private static GetAlbumsForArtistSociableTestEnvironment ForCompletedArtistAlbum(
        string title = "Scenario Album",
        string? sourceAlbumId = null,
        DateTimeOffset requestTime = default) =>
        GetAlbumsForArtistSociableTestEnvironment.ForLookupDataComplete(
            requestTime,
            LookupDataCompleteArtistAlbum.Create(
                LookupDataCompleteArtistAlbumScenarios.DefaultArtistId,
                title,
                new DateOnly(2025, 2, 3),
                requestTime,
                sourceAlbumId: sourceAlbumId ?? "scenario-album"));
}
