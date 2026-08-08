using Soundtrail.Domain.Catalog.Artists;

namespace Soundtrail.Services.Tests.Unit.Sociable.GetArtist.DataAvailable.Api;

public sealed class DataAvailableTests
{
    [Fact]
    public async Task When_Requesting_Then_An_Artist_Is_Returned()
    {
        var artistId = ArtistId.From("artist-501");
        var environment = GetArtistSociableTestEnvironment.ForDataAvailable(artistId: artistId);

        var result = await environment.ProjectOnChange(
            sut => sut.Handle(environment.CreateRequest()));

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task When_Requesting_Then_The_Artist_Id_Is_Returned()
    {
        var artistId = ArtistId.From("artist-503");
        var environment = GetArtistSociableTestEnvironment.ForDataAvailable(artistId: artistId);

        var result = await environment.ProjectOnChange(
            sut => sut.Handle(environment.CreateRequest()));

        result!.ArtistId.Should().Be(artistId);
    }

    [Fact]
    public async Task When_Requesting_Then_The_Artist_Name_Is_Returned()
    {
        var artistName = "Artist 504";
        var environment = GetArtistSociableTestEnvironment.ForDataAvailable(
            response: GetArtistScenarioData.CreateResponse(artistName: artistName));

        var result = await environment.ProjectOnChange(
            sut => sut.Handle(environment.CreateRequest()));

        result!.ArtistName.Value.Should().Be(artistName);
    }

    [Fact]
    public async Task When_Requesting_Then_The_Description_Is_Returned()
    {
        var description = "Artist 505 Description";
        var environment = GetArtistSociableTestEnvironment.ForDataAvailable(
            response: GetArtistScenarioData.CreateResponse(description: description));

        var result = await environment.ProjectOnChange(
            sut => sut.Handle(environment.CreateRequest()));

        result!.Description.Should().Be(description);
    }

    [Fact]
    public async Task When_Requesting_Then_The_Image_Url_Is_Returned()
    {
        var imageUrl = "https://cdn.soundtrail.test/artists/artist-506.jpg";
        var environment = GetArtistSociableTestEnvironment.ForDataAvailable(
            response: GetArtistScenarioData.CreateResponse(imageUrl: imageUrl));

        var result = await environment.ProjectOnChange(
            sut => sut.Handle(environment.CreateRequest()));

        result!.ImageUrl.Should().Be(imageUrl);
    }

    [Fact]
    public async Task When_Requesting_Then_The_Requested_Artist_Id_Is_Read()
    {
        var artistId = ArtistId.From("artist-502");
        var environment = GetArtistSociableTestEnvironment.ForDataAvailable(artistId: artistId);

        await environment.ProjectOnChange(sut => sut.Handle(environment.CreateRequest()));

        environment.Port.RequestedArtistIds.Single().Should().Be(artistId);
    }

    [Fact]
    public async Task When_Requesting_Then_No_Enrichment_Work_Is_Scheduled()
    {
        var artistId = ArtistId.From("artist-501");
        var environment = GetArtistSociableTestEnvironment.ForDataAvailable(artistId: artistId);

        await environment.ProjectOnChange(sut => sut.Handle(environment.CreateRequest()));

        environment.SentMessages.Should().BeEmpty();
    }
}
