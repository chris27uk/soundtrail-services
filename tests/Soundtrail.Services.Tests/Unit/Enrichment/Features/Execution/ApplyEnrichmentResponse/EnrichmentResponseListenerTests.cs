using FluentAssertions;
using Soundtrail.Domain.Events;
using Soundtrail.Domain.Model;
using Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

namespace Soundtrail.Services.Tests.Unit.Enrichment.Features.Execution.ApplyEnrichmentResponse;

public sealed class EnrichmentResponseListenerTests
{
    [Fact]
    public async Task Given_A_MusicBrainz_Response_Dto_When_Handled_Then_A_Stream_Is_Created()
    {
        var env = EnrichmentResponseListenerTestEnvironment.WithAMusicBrainzResponseDto();
        await env.HandleMusicBrainzResponse();
        env.StreamStore.Streams.Should().ContainKey("mc_track_1");
    }

    [Fact]
    public async Task Given_A_MusicBrainz_Response_Dto_When_Handled_Then_A_Projection_Is_Created()
    {
        var env = EnrichmentResponseListenerTestEnvironment.WithAMusicBrainzResponseDto();
        await env.HandleMusicBrainzResponse();
        env.ProjectionStore.Projections.Should().ContainKey("mc_track_1");
    }

    [Fact]
    public async Task Given_A_MusicBrainz_Response_Dto_When_Handled_Then_A_MusicBrainz_Snapshot_Is_Saved()
    {
        var env = EnrichmentResponseListenerTestEnvironment.WithAMusicBrainzResponseDto();
        await env.HandleMusicBrainzResponse();
        env.SnapshotStore.Snapshots.Should().ContainKey("mc_track_1:MusicBrainz");
    }

    [Fact]
    public async Task Given_A_MusicBrainz_Response_Dto_When_Handled_Then_A_MinimalTrackInfoDiscovered_Fact_Is_Stored()
    {
        var env = EnrichmentResponseListenerTestEnvironment.WithAMusicBrainzResponseDto();
        await env.HandleMusicBrainzResponse();
        env.StreamStore.Streams["mc_track_1"].Facts.Should().ContainItemsAssignableTo<MinimalTrackInfoDiscovered>();
    }

    [Fact]
    public async Task Given_A_PlaybackReferences_Response_Dto_When_Handled_Then_The_Track_Becomes_Playable()
    {
        var env = EnrichmentResponseListenerTestEnvironment.WithAPlaybackReferencesResponseAfterCanonicalMetadata();
        await env.HandlePlaybackReferencesResponseAfterCanonicalMetadata();
        env.ProjectionStore.Projections["mc_track_1"].IsPlayable.Should().BeTrue();
    }

    [Fact]
    public async Task Given_A_PlaybackReferences_Response_Dto_When_Handled_Then_The_Apple_Reference_Confidence_Is_Verified()
    {
        var env = EnrichmentResponseListenerTestEnvironment.WithAPlaybackReferencesResponseAfterCanonicalMetadata();
        await env.HandlePlaybackReferencesResponseAfterCanonicalMetadata();
        env.ProjectionStore.Projections["mc_track_1"].Apple!.Confidence.Should().Be(ReferenceConfidence.Verified);
    }

    [Fact]
    public async Task Given_A_Duplicate_Response_Dto_When_Handled_Then_Only_A_Single_CommandId_Is_Recorded()
    {
        var env = EnrichmentResponseListenerTestEnvironment.WithADuplicateMusicBrainzResponseDto();
        await env.HandleDuplicateMusicBrainzResponse();
        env.StreamStore.Streams["mc_track_1"].AppliedCommandIds.Should().ContainSingle();
    }
}
