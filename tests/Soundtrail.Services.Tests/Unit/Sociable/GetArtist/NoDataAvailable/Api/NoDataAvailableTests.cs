using Soundtrail.Domain.Catalog.Artists;

namespace Soundtrail.Services.Tests.Unit.Sociable.GetArtist.NoDataAvailable.Api;

public sealed class NoDataAvailableTests
{
    [Fact]
    public async Task When_Requesting_Then_No_Artist_Is_Returned()
    {
        var artistId = ArtistId.From("artist-602");
        var environment = GetArtistSociableTestEnvironment.ForNoDataAvailable(artistId);

        var result = await environment.ProjectOnChange(
            sut => sut.Handle(environment.CreateRequest()));

        result.Should().BeNull();
    }

    [Fact]
    public async Task When_Requesting_Then_The_Requested_Artist_Id_Is_Read()
    {
        var artistId = ArtistId.From("artist-602");
        var environment = GetArtistSociableTestEnvironment.ForNoDataAvailable(artistId);

        await environment.ProjectOnChange(sut => sut.Handle(environment.CreateRequest()));

        environment.Port.RequestedArtistIds.Single().Should().Be(artistId);
    }

    [Fact]
    public async Task When_Requesting_Then_No_Enrichment_Work_Is_Scheduled()
    {
        var artistId = ArtistId.From("artist-602");
        var environment = GetArtistSociableTestEnvironment.ForNoDataAvailable(artistId);

        await environment.ProjectOnChange(sut => sut.Handle(environment.CreateRequest()));

        environment.SentMessages.Should().BeEmpty();
    }
}
