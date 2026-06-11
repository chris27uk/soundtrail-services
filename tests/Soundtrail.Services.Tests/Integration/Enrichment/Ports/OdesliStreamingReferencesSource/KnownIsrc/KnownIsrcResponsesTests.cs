using FluentAssertions;
using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Model;
using Soundtrail.Domain.Responses;

namespace Soundtrail.Services.Tests.Integration.Enrichment.Ports.OdesliStreamingReferencesSource.KnownIsrc;

public sealed class KnownIsrcResponsesTests
{
    [Theory]
    [MemberData(nameof(OdesliStreamingReferencesSourceContractModes.All), MemberType = typeof(OdesliStreamingReferencesSourceContractModes))]
    public async Task Given_A_Known_Isrc_When_Streaming_References_Are_Looked_Up_Then_All_Supported_Providers_Are_Returned(OdesliStreamingReferencesSourceMode mode)
    {
        var env = OdesliStreamingReferencesSourceTestEnvironment.Create(mode);
        env.Seed(
            MusicSearchTerm.ByIsrc("isrc-1"),
            new ExternalReference(ProviderName.AppleMusic, new Uri("https://music.apple.com/us/song/song-a?i=apple-1"), "apple-1", ReferenceConfidence.Verified),
            new ExternalReference(ProviderName.YoutubeMusic, new Uri("https://music.youtube.com/watch?v=yt-1"), "yt-1", ReferenceConfidence.Verified),
            new ExternalReference(ProviderName.Spotify, new Uri("https://open.spotify.com/track/spotify-1"), "spotify-1", ReferenceConfidence.Verified));

        var actual = await env.Source.GetReferenceToMusicTrack(MusicSearchTerm.ByIsrc("isrc-1"), CancellationToken.None);

        actual.Should().BeEquivalentTo(
            [
                new ExternalReference(ProviderName.YoutubeMusic, new Uri("https://music.youtube.com/watch?v=yt-1"), "yt-1", ReferenceConfidence.Verified),
                new ExternalReference(ProviderName.Spotify, new Uri("https://open.spotify.com/track/spotify-1"), "spotify-1", ReferenceConfidence.Verified),
                new ExternalReference(ProviderName.AppleMusic, new Uri("https://music.apple.com/us/song/song-a?i=apple-1"), "apple-1", ReferenceConfidence.Verified)
            ]);
    }
}
