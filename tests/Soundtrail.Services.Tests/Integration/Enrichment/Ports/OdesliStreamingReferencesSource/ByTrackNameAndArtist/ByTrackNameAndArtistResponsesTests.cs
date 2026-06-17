using FluentAssertions;
using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Model;
using Soundtrail.Domain.Responses;

namespace Soundtrail.Services.Tests.Integration.Enrichment.Ports.OdesliStreamingReferencesSource.ByTrackNameAndArtist;

public sealed class ByTrackNameAndArtistResponsesTests
{
    [Theory]
    [MemberData(nameof(OdesliStreamingReferencesSourceContractModes.All), MemberType = typeof(OdesliStreamingReferencesSourceContractModes))]
    public async Task Given_A_ByTrackNameAndArtist_Lookup_When_Streaming_References_Are_Looked_Up_Then_All_Supported_Providers_Are_Returned(OdesliStreamingReferencesSourceMode mode)
    {
        using var env = OdesliStreamingReferencesSourceTestEnvironment.Create(mode);
        env.Seed(
            MusicSearchTerm.ByTrackArtistAlbum("Song A", "Artist A", "Album A"),
            new ExternalReference(ProviderName.YoutubeMusic, new Uri("https://music.youtube.com/watch?v=yt-2"), "yt-2"));

        var actual = await env.Source.GetReferenceToMusicTrack(
            MusicSearchTerm.ByTrackArtistAlbum("Song A", "Artist A", "Album A"),
            CancellationToken.None);

        actual.Should().BeEquivalentTo(
            [new ExternalReference(ProviderName.YoutubeMusic, new Uri("https://music.youtube.com/watch?v=yt-2"), "yt-2")]);
    }
}
