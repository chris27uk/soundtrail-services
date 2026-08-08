using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForArtist.Contract;

namespace Soundtrail.Services.Tests.Unit.Sociable.GetTracksForArtist.NoTracksFound.Api;

public sealed class NoTracksFoundTests
{
    [Fact]
    public async Task Given_Lookup_Found_No_Tracks_When_Requesting_Then_Artist_Id_Is_Returned()
    {
        var environment = await GetTracksForArtistSociableTestEnvironment.ForExistingCompletedEmptyLookup();

        var response = await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForArtistRequest(environment.ArtistId)));

        response!.ArtistId.Should().Be(environment.ArtistId);
    }

    [Fact]
    public async Task Given_Lookup_Found_No_Tracks_When_Requesting_Then_Artist_Name_Is_Empty()
    {
        var environment = await GetTracksForArtistSociableTestEnvironment.ForExistingCompletedEmptyLookup();

        var response = await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForArtistRequest(environment.ArtistId)));

        response!.ArtistName.Should().Be(ArtistName.Empty);
    }

    [Fact]
    public async Task Given_Lookup_Found_No_Tracks_When_Requesting_Then_No_Tracks_Are_Returned()
    {
        var environment = await GetTracksForArtistSociableTestEnvironment.ForExistingCompletedEmptyLookup();

        var response = await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForArtistRequest(environment.ArtistId)));

        response!.Tracks.Should().BeEmpty();
    }

    [Fact]
    public async Task Given_Lookup_Found_No_Tracks_When_Requesting_Then_Discovery_Is_Completed()
    {
        var environment = await GetTracksForArtistSociableTestEnvironment.ForExistingCompletedEmptyLookup();

        var response = await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForArtistRequest(environment.ArtistId)));

        response!.Discovery!.Status.Should().Be("completed");
    }

    [Fact]
    public async Task Given_Lookup_Found_No_Tracks_When_Requesting_Then_Discovery_Reason_Is_Lookup_Completed()
    {
        var environment = await GetTracksForArtistSociableTestEnvironment.ForExistingCompletedEmptyLookup();

        var response = await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForArtistRequest(environment.ArtistId)));

        response!.Discovery!.Reason.Should().Be("Lookup completed.");
    }

    [Fact]
    public async Task Given_Lookup_Found_No_Tracks_When_Requesting_Then_Discovery_Has_High_Priority()
    {
        var environment = await GetTracksForArtistSociableTestEnvironment.ForExistingCompletedEmptyLookup();

        var response = await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForArtistRequest(environment.ArtistId)));

        response!.Discovery!.Priority.Should().Be(LookupPriorityBand.High);
    }
}
