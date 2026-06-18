using FluentAssertions;
using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Model;
using Soundtrail.Domain.Responses;
using Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

namespace Soundtrail.Services.Tests.Unit.Enrichment.Features.Execution.PlaybackReferencesLookupExecution;

public sealed class PlaybackReferencesLookupExecutionHandlerTests
{
    [Fact]
    public async Task Given_An_Isrc_Lookup_When_Handled_Then_The_Playback_References_Are_Returned()
    {
        var env = PlaybackReferencesLookupExecutionHandlerTestEnvironment.Create();
        env.Seed(
            MusicSearchTerm.ByIsrc("isrc-1"),
            new ExternalReference(ProviderName.AppleMusic, new Uri("https://music.apple.com/us/song/apple-1?i=apple-1"), "apple-1"),
            new ExternalReference(ProviderName.Spotify, new Uri("https://open.spotify.com/track/spotify-1"), "spotify-1"));

        var result = await env.HandleNewExecutionCommand();

        result.Outcome.Should().Be(LookupExecutionOutcome.Completed);
        result.Response!.SourceProvider.Should().Be(ProviderName.Odesli);
        result.Response.References.Should().HaveCount(2);
        result.Response.FailedProviders.Should().ContainSingle()
            .Which.Provider.Should().Be(ProviderName.YoutubeMusic);
    }

    [Fact]
    public async Task Given_A_ByTrackNameAndArtist_Lookup_When_Handled_Then_The_Name_Based_Input_Is_Used()
    {
        var env = PlaybackReferencesLookupExecutionHandlerTestEnvironment.Create();
        env.Seed(
            MusicSearchTerm.ByTrackArtistAlbum("Song A", "Artist A", "Album A"),
            new ExternalReference(ProviderName.YoutubeMusic, new Uri("https://music.youtube.com/watch?v=yt-1"), "yt-1"));

        var result = await env.HandleNewExecutionCommand(MusicSearchTerm.ByTrackArtistAlbum("Song A", "Artist A", "Album A"));

        result.Response!.References.Should().ContainSingle().Which.ExternalId.Should().Be("yt-1");
        result.Response.FailedProviders.Select(x => x.Provider.Value).Should().BeEquivalentTo(
            ProviderName.AppleMusic.Value,
            ProviderName.Spotify.Value);
    }

    [Fact]
    public async Task Given_A_Duplicate_Execution_Command_When_Handled_Then_No_Budget_Is_Reserved_And_No_Response_Is_Returned()
    {
        var env = PlaybackReferencesLookupExecutionHandlerTestEnvironment.Create();

        var result = await env.HandleDuplicateExecutionCommand();

        result.Outcome.Should().Be(LookupExecutionOutcome.Duplicate);
        result.Response.Should().BeNull();
        env.SourceBudget.Requests.Should().ContainSingle();
    }

    [Fact]
    public async Task Given_A_Budget_Rejection_When_Handled_Then_The_Lookup_Is_Deferred_And_The_Source_Is_Not_Called()
    {
        var env = PlaybackReferencesLookupExecutionHandlerTestEnvironment.Create();
        env.SourceBudget.Reject(
            ProviderName.Odesli,
            new DateTimeOffset(2026, 6, 8, 12, 1, 0, TimeSpan.Zero),
            "Odesli budget temporarily unavailable");

        var result = await env.HandleNewExecutionCommand();

        result.Outcome.Should().Be(LookupExecutionOutcome.Deferred);
        result.Response.Should().BeNull();
        result.Reason.Should().Be("Odesli budget temporarily unavailable");
        env.GetMusicTrackReference.SearchTerms.Should().BeEmpty();
    }

    [Fact]
    public async Task Given_A_Failing_Execution_Command_When_Handled_Then_A_Failed_Outcome_Is_Returned()
    {
        var env = PlaybackReferencesLookupExecutionHandlerTestEnvironment.Create();
        env.Throw(new InvalidOperationException("boom"));

        var result = await env.HandleNewExecutionCommand();

        result.Outcome.Should().Be(LookupExecutionOutcome.Failed);
        result.Reason.Should().Be("Lookup failed");
        result.Response.Should().BeNull();
    }
}
