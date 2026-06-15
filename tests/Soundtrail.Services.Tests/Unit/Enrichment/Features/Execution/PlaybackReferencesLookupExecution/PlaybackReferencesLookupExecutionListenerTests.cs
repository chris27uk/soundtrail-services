using FluentAssertions;
using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Contracts.IntegrationMessaging.Responses;
using Soundtrail.Domain.Discovery;
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
            new ExternalReference(ProviderName.AppleMusic, new Uri("https://music.apple.com/us/song/apple-1?i=apple-1"), "apple-1"),
            new ExternalReference(ProviderName.Spotify, new Uri("https://open.spotify.com/track/spotify-1"), "spotify-1"));

        var message = (EnrichmentResponseDto)(await env.HandleNewExecutionCommand()).Single();

        message.SourceProvider.Should().Be(ProviderName.Odesli.Value);
        message.References.Should().HaveCount(2);
        message.FailedProviders.Should().ContainSingle()
            .Which.Provider.Should().Be(ProviderName.YoutubeMusic.Value);
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
            new ExternalReference(ProviderName.YoutubeMusic, new Uri("https://music.youtube.com/watch?v=yt-1"), "yt-1"));

        var message = (EnrichmentResponseDto)(await env.HandleNewExecutionCommand(command)).Single();

        message.References.Should().ContainSingle().Which.ExternalId.Should().Be("yt-1");
        message.FailedProviders.Select(x => x.Provider).Should().BeEquivalentTo(
            ProviderName.AppleMusic.Value,
            ProviderName.Spotify.Value);
    }

    [Fact]
    public async Task Given_A_Duplicate_Execution_Command_Dto_When_Handled_Then_No_Messages_Are_Returned()
    {
        var env = PlaybackReferencesLookupExecutionListenerTestEnvironment.WithADuplicateExecutionCommandDto();
        var duplicate = await env.HandleDuplicateExecutionCommand();
        duplicate.Should().BeEmpty();
    }

    [Fact]
    public async Task Given_A_New_Execution_Command_Dto_When_Handled_Then_CatalogSearch_Status_Is_Projected_As_InProgress()
    {
        var env = PlaybackReferencesLookupExecutionListenerTestEnvironment.WithANewExecutionCommandDto();
        env.Seed(MusicSearchTerm.ByIsrc("isrc-1"));

        await env.HandleNewExecutionCommand();

        env.DiscoveryStatus.Updates["search:track:rare unknown song"].Status.Should().Be(CatalogSearchLifecycleStatus.InProgress);
    }

    [Fact]
    public async Task Given_A_Failing_Execution_Command_Dto_When_Handled_Then_CatalogSearch_Status_Is_Projected_As_Failed()
    {
        var env = PlaybackReferencesLookupExecutionListenerTestEnvironment.WithANewExecutionCommandDto();
        env.Throw(new InvalidOperationException("boom"));

        Func<Task> act = () => env.HandleNewExecutionCommand();

        await act.Should().ThrowAsync<InvalidOperationException>();
        env.DiscoveryStatus.Updates["search:track:rare unknown song"].Status.Should().Be(CatalogSearchLifecycleStatus.Failed);
    }
}
