using Soundtrail.Domain.Common;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForAlbum.Contract;

namespace Soundtrail.Services.Tests.Unit.Sociable.Features.GetTracksForAlbum.Scenarios.NoTracksFound.Api;

public sealed class NoTracksFoundTests
{
    [Fact]
    public async Task Given_Lookup_Found_No_Tracks_When_Requesting_Then_Album_Id_Is_Returned()
    {
        var environment = await GetTracksForAlbumSociableTestEnvironment.ForExistingCompletedEmptyLookup();

        var response = await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForAlbumRequest(environment.AlbumId)));

        response!.AlbumId.Should().Be(environment.AlbumId);
    }

    [Fact]
    public async Task Given_Lookup_Found_No_Tracks_When_Requesting_Then_Artist_Id_Is_Returned()
    {
        var environment = await GetTracksForAlbumSociableTestEnvironment.ForExistingCompletedEmptyLookup();

        var response = await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForAlbumRequest(environment.AlbumId)));

        response!.ArtistId.Value.Should().Be(environment.AlbumId.ArtistId);
    }

    [Fact]
    public async Task Given_Lookup_Found_No_Tracks_When_Requesting_Then_Album_Title_Is_Empty()
    {
        var environment = await GetTracksForAlbumSociableTestEnvironment.ForExistingCompletedEmptyLookup();

        var response = await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForAlbumRequest(environment.AlbumId)));

        response!.AlbumTitle.Should().Be(string.Empty);
    }

    [Fact]
    public async Task Given_Lookup_Found_No_Tracks_When_Requesting_Then_No_Tracks_Are_Returned()
    {
        var environment = await GetTracksForAlbumSociableTestEnvironment.ForExistingCompletedEmptyLookup();

        var response = await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForAlbumRequest(environment.AlbumId)));

        response!.Tracks.Should().BeEmpty();
    }

    [Fact]
    public async Task Given_Lookup_Found_No_Tracks_When_Requesting_Then_Discovery_Is_Completed()
    {
        var environment = await GetTracksForAlbumSociableTestEnvironment.ForExistingCompletedEmptyLookup();

        var response = await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForAlbumRequest(environment.AlbumId)));

        response!.Discovery!.Status.Should().Be("completed");
    }

    [Fact]
    public async Task Given_Lookup_Found_No_Tracks_When_Requesting_Then_Discovery_Reason_Is_Lookup_Completed()
    {
        var environment = await GetTracksForAlbumSociableTestEnvironment.ForExistingCompletedEmptyLookup();

        var response = await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForAlbumRequest(environment.AlbumId)));

        response!.Discovery!.Reason.Should().Be("Lookup completed.");
    }

    [Fact]
    public async Task Given_Lookup_Found_No_Tracks_When_Requesting_Then_Discovery_Has_High_Priority()
    {
        var environment = await GetTracksForAlbumSociableTestEnvironment.ForExistingCompletedEmptyLookup();

        var response = await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForAlbumRequest(environment.AlbumId)));

        response!.Discovery!.Priority.Should().Be(LookupPriorityBand.High);
    }
}
