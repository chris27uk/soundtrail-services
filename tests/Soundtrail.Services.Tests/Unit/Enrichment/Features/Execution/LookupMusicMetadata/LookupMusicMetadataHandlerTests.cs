using FluentAssertions;
using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Model;
using Soundtrail.Domain.Responses;
using Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

namespace Soundtrail.Services.Tests.Unit.Enrichment.Features.Execution.LookupMusicMetadata;

public sealed class LookupMusicMetadataHandlerTests
{
    [Fact]
    public async Task Given_A_New_Execution_Command_When_Handled_Then_A_Completed_Response_Is_Returned()
    {
        var env = LookupMusicMetadataHandlerTestEnvironment.Create();

        var result = await env.HandleNewExecutionCommand();

        result.Outcome.Status.Should().Be(MusicCatalogLookupOutcomeStatus.Completed);
        result.MusicCatalogMetadataFetched.Should().NotBeNull();
        result.MusicCatalogMetadataFetched!.SourceProvider.Should().Be(ProviderName.MusicBrainz);
        result.MusicCatalogMetadataFetched.MusicCatalogId.Should().Be(MusicCatalogId.From("mc_track_1"));
    }

    [Fact]
    public async Task Given_A_Duplicate_Execution_Command_When_Handled_Then_A_Duplicate_Outcome_Is_Returned_And_The_Source_Is_Not_Called_Twice()
    {
        var env = LookupMusicMetadataHandlerTestEnvironment.Create();

        var result = await env.HandleDuplicateExecutionCommand();

        result.Outcome.Status.Should().Be(MusicCatalogLookupOutcomeStatus.Duplicate);
        result.MusicCatalogMetadataFetched.Should().BeNull();
        env.Admission.Requests.Should().HaveCount(2);
        env.Metadata.Lookups.Should().ContainSingle();
    }

    [Fact]
    public async Task Given_An_Isrc_Lookup_When_Handled_Then_MusicBrainz_Is_Queried_By_Isrc()
    {
        var env = LookupMusicMetadataHandlerTestEnvironment.Create();
        env.SeedMusicBrainzIsrc("isrc-1", new SongMetadata("Song A", "Artist A", "isrc-1", "mbid-1", 123000, "Album A", new DateOnly(2004, 6, 7), "mb-artist-1", "mb-release-1"));

        var result = await env.HandleNewExecutionCommand(MusicSearchCriteria.ByIsrc("isrc-1"));

        result.MusicCatalogMetadataFetched!.Metadata.Should().BeEquivalentTo(new SongMetadata("Song A", "Artist A", "isrc-1", "mbid-1", 123000, "Album A", new DateOnly(2004, 6, 7), "mb-artist-1", "mb-release-1"));
        env.Metadata.Lookups.Should().ContainSingle().Which.Should().Be("isrc:isrc-1");
    }

    [Fact]
    public async Task Given_A_Track_Artist_And_Album_Lookup_When_Handled_Then_MusicBrainz_Is_Queried_By_Names()
    {
        var env = LookupMusicMetadataHandlerTestEnvironment.Create();
        env.SeedMusicBrainzNames("Song A", "Artist A", "Album A", new SongMetadata("Song A", "Artist A", null, "mbid-1", 123000, "Album A", new DateOnly(2004, 6, 7), "mb-artist-1", "mb-release-1"));

        var result = await env.HandleNewExecutionCommand(MusicSearchCriteria.ByTrackArtistAlbum("Song A", "Artist A", "Album A"));

        result.MusicCatalogMetadataFetched!.Metadata.Should().BeEquivalentTo(new SongMetadata("Song A", "Artist A", null, "mbid-1", 123000, "Album A", new DateOnly(2004, 6, 7), "mb-artist-1", "mb-release-1"));
        env.Metadata.Lookups.Should().ContainSingle().Which.Should().StartWith("names:");
    }

    [Fact]
    public async Task Given_A_Budget_Rejection_When_Handled_Then_The_Lookup_Is_Deferred_And_The_Source_Is_Not_Called()
    {
        var env = LookupMusicMetadataHandlerTestEnvironment.Create();
        env.Admission.Reject(
            ProviderName.MusicBrainz,
            new DateTimeOffset(2026, 6, 8, 12, 1, 0, TimeSpan.Zero),
            "MusicBrainz budget temporarily unavailable");

        var result = await env.HandleNewExecutionCommand();

        result.Outcome.Status.Should().Be(MusicCatalogLookupOutcomeStatus.Deferred);
        result.MusicCatalogMetadataFetched.Should().BeNull();
        result.Outcome.Reason.Should().Be("MusicBrainz budget temporarily unavailable");
        env.Metadata.Lookups.Should().BeEmpty();
    }

    [Fact]
    public async Task Given_A_Failing_Execution_Command_When_Handled_Then_A_Failed_Outcome_Is_Returned()
    {
        var env = LookupMusicMetadataHandlerTestEnvironment.Create();
        env.Throw(new InvalidOperationException("boom"));

        var result = await env.HandleNewExecutionCommand();

        result.Outcome.Status.Should().Be(MusicCatalogLookupOutcomeStatus.Failed);
        result.Outcome.Reason.Should().Be("Lookup failed");
        result.MusicCatalogMetadataFetched.Should().BeNull();
    }
}
