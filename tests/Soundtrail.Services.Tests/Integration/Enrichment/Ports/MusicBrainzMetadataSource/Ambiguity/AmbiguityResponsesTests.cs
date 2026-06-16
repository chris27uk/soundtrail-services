using FluentAssertions;
using Soundtrail.Domain.Model;

namespace Soundtrail.Services.Tests.Integration.Enrichment.Ports.MusicBrainzMetadataSource.Ambiguity;

public sealed class AmbiguityResponsesTests
{
    [Theory]
    [MemberData(nameof(MusicBrainzMetadataSourceContractModes.All), MemberType = typeof(MusicBrainzMetadataSourceContractModes))]
    public async Task Given_An_Ambiguous_Name_Lookup_When_Metadata_Is_Looked_Up_Then_No_Metadata_Is_Returned(MusicBrainzMetadataSourceMode mode)
    {
        using var env = MusicBrainzMetadataSourceTestEnvironment.Create(mode);
        var lookup = MusicSearchTerm.ByTrackArtistAlbum("Song A", "Artist A", "Album A");
        env.SeedAmbiguous(lookup);

        var actual = await env.Source.GetMetadataAsync(lookup, CancellationToken.None);

        actual.Should().BeNull();
    }

    [Theory]
    [MemberData(nameof(MusicBrainzMetadataSourceContractModes.All), MemberType = typeof(MusicBrainzMetadataSourceContractModes))]
    public async Task Given_An_Ambiguous_Isrc_Lookup_When_Metadata_Is_Looked_Up_Then_No_Metadata_Is_Returned(MusicBrainzMetadataSourceMode mode)
    {
        using var env = MusicBrainzMetadataSourceTestEnvironment.Create(mode);
        var lookup = MusicSearchTerm.ByIsrc("isrc-1");
        env.SeedAmbiguous(lookup);

        var actual = await env.Source.GetMetadataAsync(lookup, CancellationToken.None);

        actual.Should().BeNull();
    }
}
