using Soundtrail.Domain.Catalog;

namespace Soundtrail.Services.Tests.Unit.GetTracksForArtist;

public sealed class ArtistTracksDoNotExistTests
{
    [Fact]
    public async Task Given_Missing_Artist_Tracks_When_Requesting_The_Artist_Tracks_Then_No_Artist_Tracks_Are_Returned()
    {
        var environment = GetTracksForArtistMissingUnitTestEnvironment.ForMissingArtistTracks();

        var result = await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        result.Should().BeNull();
    }

    [Fact]
    public async Task Given_Missing_Artist_Tracks_When_Requesting_The_Artist_Tracks_Then_The_Requested_Artist_Id_Is_Read()
    {
        var artistId = ArtistId.From("artist-2307");
        var environment = GetTracksForArtistMissingUnitTestEnvironment.ForMissingArtistTracks(artistId);

        await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        environment.Port.RequestedArtistIds.Single().Should().Be(artistId);
    }
}
