using FluentAssertions;
using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Events;
using Soundtrail.Domain.Model;
using Soundtrail.Domain.Responses;
using Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

namespace Soundtrail.Services.Tests.Unit.Enrichment.Features.Execution.MusicBrainzLookupExecution;

public sealed class MusicBrainzLookupExecutionHandlerTests
{
    [Fact]
    public async Task Given_A_New_Execution_Command_When_Handled_Then_A_Completed_Response_Is_Returned()
    {
        var env = MusicBrainzLookupExecutionHandlerTestEnvironment.Create();

        var result = await env.HandleNewExecutionCommand();

        result.Outcome.Should().Be(LookupExecutionOutcome.Completed);
        result.Response.Should().NotBeNull();
        result.Response!.SourceProvider.Should().Be(ProviderName.MusicBrainz);
        result.Response.MusicCatalogId.Should().Be(MusicCatalogId.From("mc_track_1"));
    }

    [Fact]
    public async Task Given_A_Duplicate_Execution_Command_When_Handled_Then_No_Budget_Is_Reserved_And_No_Response_Is_Returned()
    {
        var env = MusicBrainzLookupExecutionHandlerTestEnvironment.Create();

        var result = await env.HandleDuplicateExecutionCommand();

        result.Outcome.Should().Be(LookupExecutionOutcome.Duplicate);
        result.Response.Should().BeNull();
        env.SourceBudget.Requests.Should().ContainSingle();
        env.DiscoveryRepository
            .GetStoredEvents(CatalogSearchCriteria.Search("track", "rare unknown song"))
            .Should().ContainSingle().Which.Should().BeOfType<DiscoveryStarted>();
    }

    [Fact]
    public async Task Given_An_Isrc_Lookup_When_Handled_Then_MusicBrainz_Is_Queried_By_Isrc()
    {
        var env = MusicBrainzLookupExecutionHandlerTestEnvironment.Create();
        env.SeedMusicBrainzIsrc("isrc-1", new SongMetadata("Song A", "Artist A", "isrc-1", "mbid-1", 123000));

        var result = await env.HandleNewExecutionCommand(MusicSearchTerm.ByIsrc("isrc-1"));

        result.Response!.Metadata.Should().BeEquivalentTo(new SongMetadata("Song A", "Artist A", "isrc-1", "mbid-1", 123000));
        env.Metadata.Lookups.Should().ContainSingle().Which.Should().Be("isrc:isrc-1");
    }

    [Fact]
    public async Task Given_A_Track_Artist_And_Album_Lookup_When_Handled_Then_MusicBrainz_Is_Queried_By_Names()
    {
        var env = MusicBrainzLookupExecutionHandlerTestEnvironment.Create();
        env.SeedMusicBrainzNames("Song A", "Artist A", "Album A", new SongMetadata("Song A", "Artist A", null, "mbid-1", 123000));

        var result = await env.HandleNewExecutionCommand(MusicSearchTerm.ByTrackArtistAlbum("Song A", "Artist A", "Album A"));

        result.Response!.Metadata.Should().BeEquivalentTo(new SongMetadata("Song A", "Artist A", null, "mbid-1", 123000));
        env.Metadata.Lookups.Should().ContainSingle().Which.Should().StartWith("names:");
    }

    [Fact]
    public async Task Given_A_Budget_Rejection_When_Handled_Then_The_Lookup_Is_Deferred_And_The_Source_Is_Not_Called()
    {
        var env = MusicBrainzLookupExecutionHandlerTestEnvironment.Create();
        env.SourceBudget.Reject(
            ProviderName.MusicBrainz,
            new DateTimeOffset(2026, 6, 8, 12, 1, 0, TimeSpan.Zero),
            "MusicBrainz budget temporarily unavailable");

        var result = await env.HandleNewExecutionCommand();

        result.Outcome.Should().Be(LookupExecutionOutcome.Deferred);
        result.Response.Should().BeNull();
        env.Metadata.Lookups.Should().BeEmpty();
        env.DiscoveryRepository
            .GetStoredEvents(CatalogSearchCriteria.Search("track", "rare unknown song"))
            .Last().Should().BeOfType<DiscoveryDeferred>();
    }

    [Fact]
    public async Task Given_A_New_Execution_Command_When_Handled_Then_CatalogSearch_Status_Is_Projected_As_InProgress()
    {
        var env = MusicBrainzLookupExecutionHandlerTestEnvironment.Create();

        await env.HandleNewExecutionCommand();

        env.DiscoveryRepository
            .GetStoredEvents(CatalogSearchCriteria.Search("track", "rare unknown song"))
            .Last().Should().BeOfType<DiscoveryStarted>();
    }

    [Fact]
    public async Task Given_A_Failing_Execution_Command_When_Handled_Then_CatalogSearch_Status_Is_Projected_As_Failed()
    {
        var env = MusicBrainzLookupExecutionHandlerTestEnvironment.Create();
        env.Throw(new InvalidOperationException("boom"));

        Func<Task> act = async () => await env.HandleNewExecutionCommand();

        await act.Should().ThrowAsync<InvalidOperationException>();
        env.DiscoveryRepository
            .GetStoredEvents(CatalogSearchCriteria.Search("track", "rare unknown song"))
            .Last().Should().BeOfType<DiscoveryFailed>();
    }
}
