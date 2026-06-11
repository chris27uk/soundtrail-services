using FluentAssertions;
using Soundtrail.Contracts.Commands;
using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.Responses;
using Soundtrail.Domain.Model;
using Soundtrail.Domain.Responses;
using Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

namespace Soundtrail.Services.Tests.Unit.Enrichment.Features.Execution.PlaybackReferencesLookupExecution;

public sealed class PlaybackReferencesLookupExecutionListenerTests
{
    [Fact]
    public async Task Given_An_Isrc_Lookup_When_Handled_Then_The_Playback_References_Are_Returned()
    {
        var env = PlaybackReferencesLookupExecutionListenerTestEnvironment.WithANewExecutionCommandDto();
        env.Seed(
            MusicSearchTerm.ByIsrc("isrc-1"),
            new ExternalReference(ProviderName.AppleMusic, new Uri("https://music.apple.com/us/song/apple-1?i=apple-1"), "apple-1", ReferenceConfidence.Verified),
            new ExternalReference(ProviderName.Spotify, new Uri("https://open.spotify.com/track/spotify-1"), "spotify-1", ReferenceConfidence.Verified));

        var message = (EnrichmentResponseDto)(await env.HandleNewExecutionCommand()).Single();

        message.SourceProvider.Should().Be(ProviderName.Odesli.Value);
        message.References.Should().HaveCount(2);
    }

    [Fact]
    public async Task Given_A_ByTrackNameAndArtist_Lookup_When_Handled_Then_The_Name_Based_Input_Is_Used()
    {
        var env = PlaybackReferencesLookupExecutionListenerTestEnvironment.WithANewExecutionCommandDto();
        var command = new ResolvePlaybackReferencesCommandDto(
            CommandId.For("ResolvePlaybackReferences:mc_track_1").Value,
            "mc_track_1",
            LookupPriorityBand.High,
            new DateTimeOffset(2026, 6, 8, 12, 0, 0, TimeSpan.Zero),
            "corr-1",
            new PlaybackReferenceSearchTermDto(null, "Song A", "Artist A", "Album A"));
        env.Seed(
            MusicSearchTerm.ByTrackArtistAlbum("Song A", "Artist A", "Album A"),
            new ExternalReference(ProviderName.YoutubeMusic, new Uri("https://music.youtube.com/watch?v=yt-1"), "yt-1", ReferenceConfidence.Verified));

        var message = (EnrichmentResponseDto)(await env.HandleNewExecutionCommand(command)).Single();

        message.References.Should().ContainSingle().Which.ExternalId.Should().Be("yt-1");
    }

    [Fact]
    public async Task Given_A_Duplicate_Execution_Command_Dto_When_Handled_Then_No_Messages_Are_Returned()
    {
        var env = PlaybackReferencesLookupExecutionListenerTestEnvironment.WithADuplicateExecutionCommandDto();
        var duplicate = await env.HandleDuplicateExecutionCommand();
        duplicate.Should().BeEmpty();
    }
}
