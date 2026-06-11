using FluentAssertions;
using Soundtrail.Domain.Model;
using Soundtrail.Domain.Responses;

namespace Soundtrail.Services.Tests.Integration.Enrichment.Ports.MusicBrainzMetadataSource.ByIsrc;

public sealed class ByIsrcResponsesTests
{
    [Theory]
    [MemberData(nameof(MusicBrainzMetadataSourceContractModes.All), MemberType = typeof(MusicBrainzMetadataSourceContractModes))]
    public async Task Given_An_Isrc_Lookup_When_Metadata_Is_Looked_Up_Then_The_Matching_Metadata_Is_Returned(MusicBrainzMetadataSourceMode mode)
    {
        var env = MusicBrainzMetadataSourceTestEnvironment.Create(mode);
        var lookup = MusicSearchTerm.ByIsrc("isrc-1");
        env.Seed(lookup, new SongMetadata("Song A", "Artist A", "isrc-1", "mbid-1", 123000));

        var actual = await env.Source.GetMetadataAsync(lookup, CancellationToken.None);

        actual.Should().Be(new SongMetadata("Song A", "Artist A", "isrc-1", "mbid-1", 123000));
    }
}
