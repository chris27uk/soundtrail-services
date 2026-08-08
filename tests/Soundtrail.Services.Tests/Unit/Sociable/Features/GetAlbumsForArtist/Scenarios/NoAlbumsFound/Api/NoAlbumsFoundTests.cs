using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Common;
using Soundtrail.Services.Api.Features.Catalog.GetAlbumsForArtist.Contract;

namespace Soundtrail.Services.Tests.Unit.Sociable.Features.GetAlbumsForArtist.Scenarios.NoAlbumsFound.Api;

public sealed class NoAlbumsFoundTests
{
    [Fact]
    public async Task Given_Lookup_Found_No_Albums_When_Requesting_Then_Artist_Id_Is_Returned()
    {
        var environment = await GetAlbumsForArtistSociableTestEnvironment.ForExistingCompletedEmptyLookup();

        var response = await environment.ProjectOnChange(
            sut => sut.Handle(new GetAlbumsForArtistRequest(environment.ArtistId)));

        response!.ArtistId.Should().Be(environment.ArtistId);
    }

    [Fact]
    public async Task Given_Lookup_Found_No_Albums_When_Requesting_Then_Artist_Name_Is_Empty()
    {
        var environment = await GetAlbumsForArtistSociableTestEnvironment.ForExistingCompletedEmptyLookup();

        var response = await environment.ProjectOnChange(
            sut => sut.Handle(new GetAlbumsForArtistRequest(environment.ArtistId)));

        response!.ArtistName.Should().Be(ArtistName.Empty);
    }

    [Fact]
    public async Task Given_Lookup_Found_No_Albums_When_Requesting_Then_No_Albums_Are_Returned()
    {
        var environment = await GetAlbumsForArtistSociableTestEnvironment.ForExistingCompletedEmptyLookup();

        var response = await environment.ProjectOnChange(
            sut => sut.Handle(new GetAlbumsForArtistRequest(environment.ArtistId)));

        response!.Albums.Should().BeEmpty();
    }

    [Fact]
    public async Task Given_Lookup_Found_No_Albums_When_Requesting_Then_Discovery_Is_Completed()
    {
        var environment = await GetAlbumsForArtistSociableTestEnvironment.ForExistingCompletedEmptyLookup();

        var response = await environment.ProjectOnChange(
            sut => sut.Handle(new GetAlbumsForArtistRequest(environment.ArtistId)));

        response!.Discovery!.Status.Should().Be("completed");
    }

    [Fact]
    public async Task Given_Lookup_Found_No_Albums_When_Requesting_Then_Discovery_Reason_Is_Lookup_Completed()
    {
        var environment = await GetAlbumsForArtistSociableTestEnvironment.ForExistingCompletedEmptyLookup();

        var response = await environment.ProjectOnChange(
            sut => sut.Handle(new GetAlbumsForArtistRequest(environment.ArtistId)));

        response!.Discovery!.Reason.Should().Be("Lookup completed.");
    }

    [Fact]
    public async Task Given_Lookup_Found_No_Albums_When_Requesting_Then_Discovery_Has_High_Priority()
    {
        var environment = await GetAlbumsForArtistSociableTestEnvironment.ForExistingCompletedEmptyLookup();

        var response = await environment.ProjectOnChange(
            sut => sut.Handle(new GetAlbumsForArtistRequest(environment.ArtistId)));

        response!.Discovery!.Priority.Should().Be(LookupPriorityBand.High);
    }
}
