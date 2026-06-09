using FluentAssertions;
using Soundtrail.Contracts.Commands;
using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.Responses;
using Soundtrail.Domain.Model;
using Soundtrail.Domain.Responses;
using Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

namespace Soundtrail.Services.Tests.Unit.Enrichment.Features.Execution.MusicBrainzLookupExecution;

public sealed class MusicBrainzLookupExecutionListenerTests
{
    [Fact]
    public async Task Given_A_New_Execution_Command_Dto_When_Handled_Then_An_EnrichmentResponse_Dto_Is_Returned()
    {
        var env = MusicBrainzLookupExecutionListenerTestEnvironment.WithANewExecutionCommandDto();
        var messages = await env.HandleNewExecutionCommand();
        messages.Should().ContainSingle().Which.Should().BeOfType<EnrichmentResponseDto>();
    }

    [Fact]
    public async Task Given_A_New_Execution_Command_Dto_When_Handled_Then_The_Response_SourceProvider_Is_MusicBrainz()
    {
        var env = MusicBrainzLookupExecutionListenerTestEnvironment.WithANewExecutionCommandDto();
        var message = (EnrichmentResponseDto)(await env.HandleNewExecutionCommand()).Single();
        message.SourceProvider.Should().Be(ProviderName.MusicBrainz.Value);
    }

    [Fact]
    public async Task Given_A_New_Execution_Command_Dto_When_Handled_Then_The_Response_MusicCatalogId_Is_Preserved()
    {
        var env = MusicBrainzLookupExecutionListenerTestEnvironment.WithANewExecutionCommandDto();
        var message = (EnrichmentResponseDto)(await env.HandleNewExecutionCommand()).Single();
        message.MusicCatalogId.Should().Be("mc_track_1");
    }

    [Fact]
    public async Task Given_A_Duplicate_Execution_Command_Dto_When_Handled_Then_No_Messages_Are_Returned()
    {
        var env = MusicBrainzLookupExecutionListenerTestEnvironment.WithADuplicateExecutionCommandDto();
        var duplicate = await env.HandleDuplicateExecutionCommand();
        duplicate.Should().BeEmpty();
    }

    [Fact]
    public async Task Given_An_Isrc_Lookup_When_Handled_Then_MusicBrainz_Is_Queried_By_Isrc()
    {
        var env = MusicBrainzLookupExecutionListenerTestEnvironment.WithANewExecutionCommandDto();
        env.SeedMusicBrainzIsrc("isrc-1", new SongMetadata("Song A", "Artist A", "isrc-1", "mbid-1", 123000));

        var message = (EnrichmentResponseDto)(await env.HandleNewExecutionCommand(CanonicalMusicMetadataLookup.FromIsrc("isrc-1"))).Single();

        message.Metadata.Should().BeEquivalentTo(new SongMetadataDto("Song A", "Artist A", "isrc-1", "mbid-1", 123000));
        env.MetadataSource.Lookups.Should().ContainSingle().Which.Should().Be("isrc:isrc-1");
    }

    [Fact]
    public async Task Given_A_Track_Artist_And_Album_Lookup_When_Handled_Then_MusicBrainz_Is_Queried_By_Names()
    {
        var env = MusicBrainzLookupExecutionListenerTestEnvironment.WithANewExecutionCommandDto();
        env.SeedMusicBrainzNames("Song A", "Artist A", "Album A", new SongMetadata("Song A", "Artist A", null, "mbid-1", 123000));

        var message = (EnrichmentResponseDto)(await env.HandleNewExecutionCommand(
            CanonicalMusicMetadataLookup.FromTrackNameArtistAndAlbum("Song A", "Artist A", "Album A"))).Single();

        message.Metadata.Should().BeEquivalentTo(new SongMetadataDto("Song A", "Artist A", null, "mbid-1", 123000));
        env.MetadataSource.Lookups.Should().ContainSingle().Which.Should().StartWith("names:");
    }
}
