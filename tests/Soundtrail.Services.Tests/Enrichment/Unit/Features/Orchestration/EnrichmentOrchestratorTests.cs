using FluentAssertions;
using Soundtrail.Services.Enrichment.Features.Orchestration;
using Soundtrail.Services.Enrichment.Shared.Execution;
using Soundtrail.Services.Enrichment.Shared.Orchestration;
using Soundtrail.Services.Enrichment.Shared.Prioritisation;
using Soundtrail.Services.Enrichment.Shared.Search;
using Soundtrail.Services.Shared;
using Soundtrail.Services.Tests.Enrichment.Unit.Infrastructure;

namespace Soundtrail.Services.Tests.Enrichment.Unit.Features.Orchestration;

public sealed class EnrichmentOrchestratorTests
{
    [Fact]
    public async Task Given_A_MusicBrainz_Response_With_Discovered_Apple_Reference_When_Planning_Then_Apple_Verification_Is_Requested()
    {
        var previous = new TrackEnrichmentState();
        var current = new TrackEnrichmentState();
        current.ApplyCanonicalMetadata(new SongMetadata("Song A", "Artist A", "isrc-1", "mbid-1", 123000));
        current.ApplyReference(
            ProviderName.Apple,
            new Uri("https://music.apple.com/track/1"),
            "apple-1",
            ReferenceConfidence.Discovered,
            ProviderName.MusicBrainz);

        var orchestrator = new EnrichmentOrchestrator(new ActiveLookupWorkStoreFake());

        var result = await orchestrator.PlanAsync(
            previous,
            current,
            new EnrichmentResponse(
                CommandId.For("ResolveCanonicalMetadata:mc_track_1"),
                MusicCatalogId.From("mc_track_1"),
                ProviderName.MusicBrainz,
                LookupPriorityBand.High,
                new DateTimeOffset(2026, 6, 5, 12, 0, 0, TimeSpan.Zero),
                current.CanonicalMetadata,
                [new ExternalReference(ProviderName.Apple, new Uri("https://music.apple.com/track/1"), "apple-1", ReferenceConfidence.Discovered)],
                CorrelationId.From("corr-1")));

        result.Commands.Should().ContainSingle()
            .Which.Should().BeOfType<VerifyApplePlaybackReferenceCommand>();
        result.Events.Should().Contain(e => e is ApplePlaybackVerificationRequested);
        result.Events.Should().Contain(e => e is CanonicalMetadataResolved);
    }

    [Fact]
    public async Task Given_A_NonPlayable_Track_That_Becomes_Playable_When_Planning_Then_Playable_Milestones_Are_Emitted()
    {
        var previous = new TrackEnrichmentState();
        previous.ApplyCanonicalMetadata(new SongMetadata("Song A", "Artist A", "isrc-1", "mbid-1", 123000));

        var current = previous.Copy();
        current.ApplyReference(
            ProviderName.Apple,
            new Uri("https://music.apple.com/track/1"),
            "apple-1",
            ReferenceConfidence.Verified,
            ProviderName.Apple);

        var orchestrator = new EnrichmentOrchestrator(new ActiveLookupWorkStoreFake());

        var result = await orchestrator.PlanAsync(
            previous,
            current,
            new EnrichmentResponse(
                CommandId.For("VerifyApplePlaybackReference:mc_track_1"),
                MusicCatalogId.From("mc_track_1"),
                ProviderName.Apple,
                LookupPriorityBand.High,
                new DateTimeOffset(2026, 6, 5, 12, 5, 0, TimeSpan.Zero),
                null,
                [new ExternalReference(ProviderName.Apple, new Uri("https://music.apple.com/track/1"), "apple-1", ReferenceConfidence.Verified)],
                CorrelationId.From("corr-2")));

        result.Commands.Should().BeEmpty();
        result.Events.Should().Contain(e => e is TrackBecamePlayable);
        result.Events.Should().Contain(e => e is EnrichmentCompleted);
    }
}
