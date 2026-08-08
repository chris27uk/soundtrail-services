using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Search;
using Soundtrail.Services.Api.Features.Catalog.Search.Contract;

namespace Soundtrail.Services.Tests.Unit.Sociable.Search.LookupDataComplete.Api;

public sealed class LookupDataCompleteTests
{
    [Fact]
    public async Task Given_Lookup_Has_Completed_When_Requesting_Then_The_Query_Text_Is_Returned()
    {
        var environment = await SearchSociableTestEnvironment.ForExistingCompletedLookup(AuroraLane());

        var response = await environment.ProjectOnChange(
            sut => sut.Handle(environment.CreateRequest()));

        response!.QueryText.Should().Be(environment.SearchCriteria.Query);
    }

    [Fact]
    public async Task Given_Lookup_Has_Completed_When_Requesting_Then_The_Filter_Is_Returned()
    {
        var environment = await SearchSociableTestEnvironment.ForExistingCompletedLookup(AuroraLane());

        var response = await environment.ProjectOnChange(
            sut => sut.Handle(environment.CreateRequest()));

        response!.Filter.Should().Be(environment.SearchCriteria.SearchTypes);
    }

    [Fact]
    public async Task Given_Lookup_Has_Completed_When_Requesting_Then_Results_Are_Returned()
    {
        var artists = new[] { AuroraLane(), AuroraLaneAlias() };
        var environment = await SearchSociableTestEnvironment.ForExistingCompletedLookup(artists);

        var response = await environment.ProjectOnChange(
            sut => sut.Handle(environment.CreateRequest()));

        response!.Results.Should().HaveCount(artists.Length);
    }

    [Fact]
    public async Task Given_Lookup_Has_Completed_When_Requesting_Then_The_Music_Catalog_Id_Is_Returned()
    {
        var environment = await SearchSociableTestEnvironment.ForExistingCompletedLookup(AuroraLane());
        var expected = new CatalogItemId.Artist(LookupDataCompleteSearchScenarios.DefaultArtistId);

        var response = await environment.ProjectOnChange(
            sut => sut.Handle(environment.CreateRequest()));

        response!.Results.Single().MusicCatalogId.Should().Be(expected);
    }

    [Fact]
    public async Task Given_Lookup_Has_Completed_When_Requesting_Then_The_Result_Type_Is_Artist()
    {
        var environment = await SearchSociableTestEnvironment.ForExistingCompletedLookup(AuroraLane());

        var response = await environment.ProjectOnChange(
            sut => sut.Handle(environment.CreateRequest()));

        response!.Results.Single().ResultType.Should().Be(SearchType.Artist);
    }

    [Fact]
    public async Task Given_Lookup_Has_Completed_When_Requesting_Then_The_Title_Is_Returned()
    {
        var environment = await SearchSociableTestEnvironment.ForExistingCompletedLookup(AuroraLane());

        var response = await environment.ProjectOnChange(
            sut => sut.Handle(environment.CreateRequest()));

        response!.Results.Single().Title.Should().Be(LookupDataCompleteSearchScenarios.DefaultQuery);
    }

    [Fact]
    public async Task Given_Lookup_Has_Completed_When_Requesting_Then_The_Artist_Name_Is_Not_Set()
    {
        var environment = await SearchSociableTestEnvironment.ForExistingCompletedLookup(AuroraLane());

        var response = await environment.ProjectOnChange(
            sut => sut.Handle(environment.CreateRequest()));

        response!.Results.Single().ArtistName.Should().BeNull();
    }

    [Fact]
    public async Task Given_Lookup_Has_Completed_When_Requesting_Then_The_Album_Title_Is_Not_Set()
    {
        var environment = await SearchSociableTestEnvironment.ForExistingCompletedLookup(AuroraLane());

        var response = await environment.ProjectOnChange(
            sut => sut.Handle(environment.CreateRequest()));

        response!.Results.Single().AlbumTitle.Should().BeNull();
    }

    [Fact]
    public async Task Given_Lookup_Has_Completed_When_Requesting_Then_The_Artwork_Url_Is_Returned()
    {
        var artworkUrl = "https://cdn.soundtrail.test/artists/aurora-lane.jpg";
        var environment = await SearchSociableTestEnvironment.ForExistingCompletedLookup(AuroraLane());

        var response = await environment.ProjectOnChange(
            sut => sut.Handle(environment.CreateRequest()));

        response!.Results.Single().ArtworkUrl.Should().Be(artworkUrl);
    }

    [Fact]
    public async Task Given_Lookup_Has_Completed_When_Requesting_Then_Discovery_Is_Completed()
    {
        var environment = await SearchSociableTestEnvironment.ForExistingCompletedLookup(AuroraLane());

        var response = await environment.ProjectOnChange(
            sut => sut.Handle(environment.CreateRequest()));

        response!.Discovery!.Status.Should().Be("completed");
    }

    [Fact]
    public async Task Given_Lookup_Has_Completed_When_Requesting_Then_Discovery_Has_High_Priority()
    {
        var environment = await SearchSociableTestEnvironment.ForExistingCompletedLookup(AuroraLane());

        var response = await environment.ProjectOnChange(
            sut => sut.Handle(environment.CreateRequest()));

        response!.Discovery!.Priority.Should().Be(LookupPriorityBand.High);
    }

    private static LookupDataCompleteSearchArtist AuroraLane() =>
        LookupDataCompleteSearchScenarios.AuroraLane();

    private static LookupDataCompleteSearchArtist AuroraLaneAlias() =>
        LookupDataCompleteSearchArtist.Create(
            ArtistId.From("artist-aurora-lane-alias"),
            LookupDataCompleteSearchScenarios.DefaultQuery,
            artworkUrl: "https://cdn.soundtrail.test/artists/aurora-lane-alias.jpg");
}
