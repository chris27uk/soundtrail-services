using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Artists;

namespace Soundtrail.Services.Tests.Unit.GetArtist;

public sealed class ArtistDoesNotExistTests
{
    [Fact]
    public async Task Given_A_Missing_Artist_When_Requesting_The_Artist_Then_No_Artist_Is_Returned()
    {
        var environment = GetArtistMissingUnitTestEnvironment.ForMissingArtist();

        var result = await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        result.Should().BeNull();
    }

    [Fact]
    public async Task Given_A_Missing_Artist_When_Requesting_The_Artist_Then_The_Requested_Artist_Id_Is_Read()
    {
        var artistId = ArtistId.From("artist-602");
        var environment = GetArtistMissingUnitTestEnvironment.ForMissingArtist(artistId);

        await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        environment.Port.RequestedArtistIds.Single().Should().Be(artistId);
    }
}
