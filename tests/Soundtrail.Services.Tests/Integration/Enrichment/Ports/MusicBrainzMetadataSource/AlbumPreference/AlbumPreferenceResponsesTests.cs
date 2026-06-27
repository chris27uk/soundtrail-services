using FluentAssertions;
using Soundtrail.Domain.Model;
using Soundtrail.Domain.Responses;

namespace Soundtrail.Services.Tests.Integration.Enrichment.Ports.MusicBrainzMetadataSource.AlbumPreference;

public sealed class AlbumPreferenceResponsesTests
{
    [Theory]
    [MemberData(nameof(MusicBrainzMetadataSourceContractModes.All), MemberType = typeof(MusicBrainzMetadataSourceContractModes))]
    public async Task Given_Multiple_Name_Matches_When_One_Album_Matches_Then_The_Album_Match_Is_Returned(MusicBrainzMetadataSourceMode mode)
    {
        using var env = MusicBrainzMetadataSourceTestEnvironment.Create(mode);
        var lookup = MusicSearchCriteria.ByTrackArtistAlbum("Song A", "Artist A", "Album A");
        env.SeedPreferredMatch(lookup, new SongMetadata("Song A", "Artist A", null, "mbid-1", 123000, "Album A", new DateOnly(2004, 6, 7), "mb-artist-1", "mb-release-1"));

        var actual = await env.Source.GetMetadataAsync(lookup, CancellationToken.None);

        actual.Should().Be(new SongMetadata("Song A", "Artist A", null, "mbid-1", 123000, "Album A", new DateOnly(2004, 6, 7), "mb-artist-1", "mb-release-1"));
    }
}
