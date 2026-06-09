using FluentAssertions;
using Soundtrail.Domain.Model;
using Soundtrail.Domain.Responses;

namespace Soundtrail.Services.Tests.Integration.Enrichment.Ports.MusicBrainzMetadataSource.ByNames;

public sealed class ByNamesResponsesTests
{
    [Theory]
    [MemberData(nameof(MusicBrainzMetadataSourceContractModes.All), MemberType = typeof(MusicBrainzMetadataSourceContractModes))]
    public async Task Given_A_Track_Artist_And_Album_Lookup_When_Metadata_Is_Looked_Up_Then_The_Matching_Metadata_Is_Returned(MusicBrainzMetadataSourceMode mode)
    {
        var env = MusicBrainzMetadataSourceTestEnvironment.Create(mode);
        var lookup = CanonicalMusicMetadataLookup.FromTrackNameArtistAndAlbum("Song A", "Artist A", "Album A");
        env.Seed(lookup, new SongMetadata("Song A", "Artist A", null, "mbid-1", 123000));

        var actual = await env.Source.GetMetadataAsync(lookup, CancellationToken.None);

        actual.Should().Be(new SongMetadata("Song A", "Artist A", null, "mbid-1", 123000));
    }
}
