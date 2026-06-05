using FluentAssertions;
using Soundtrail.Services.Enrichment.Features.Execution.ApplyEnrichmentResponse;
using Soundtrail.Services.Enrichment.Features.Orchestration;
using Soundtrail.Services.Enrichment.Shared.Execution;
using Soundtrail.Services.Enrichment.Shared.Orchestration;
using Soundtrail.Services.Enrichment.Shared.Prioritisation;
using Soundtrail.Services.Enrichment.Shared.Search;
using Soundtrail.Services.Shared;
using Soundtrail.Services.Tests.Enrichment.Unit.Infrastructure;

namespace Soundtrail.Services.Tests.Enrichment.Unit.Features.Execution.ApplyEnrichmentResponse;

public sealed class ApplyEnrichmentResponseHandlerTests
{
    [Fact]
    public async Task Given_A_MusicBrainz_Response_When_Handled_Then_Canonical_Metadata_And_Cross_Provider_References_Are_Applied()
    {
        var appliedStore = new AppliedEnrichmentResponseStoreFake();
        var trackStore = new TrackEnrichmentWriteStoreFake();
        var handler = new ApplyEnrichmentResponseHandler(
            appliedStore,
            trackStore,
            new EnrichmentOrchestrator(new ActiveLookupWorkStoreFake()));

        var result = await handler.Handle(
            new EnrichmentResponse(
                CommandId.For("ResolveCanonicalMetadata:mc_track_1"),
                MusicCatalogId.From("mc_track_1"),
                ProviderName.MusicBrainz,
                LookupPriorityBand.High,
                new DateTimeOffset(2026, 6, 5, 12, 0, 0, TimeSpan.Zero),
                new SongMetadata("Song A", "Artist A", "isrc-1", "mbid-1", 123000),
                [
                    new ExternalReference(ProviderName.Apple, new Uri("https://music.apple.com/track/1"), "apple-1", ReferenceConfidence.Discovered),
                    new ExternalReference(ProviderName.MusicBrainz, new Uri("https://musicbrainz.org/recording/1"), "mbid-1", ReferenceConfidence.Verified)
                ],
                CorrelationId.From("corr-1")));

        var state = trackStore.States["mc_track_1"];
        state.CanonicalMetadata.Should().NotBeNull();
        state.CanonicalMetadata!.Title.Should().Be("Song A");
        state.Apple.Should().NotBeNull();
        state.Apple!.ExternalId.Should().Be("apple-1");
        state.Apple.SourceProvider.Should().Be(ProviderName.MusicBrainz);
        state.MusicBrainz.Should().NotBeNull();
        appliedStore.AppliedCommandIds.Should().Contain(CommandId.For("ResolveCanonicalMetadata:mc_track_1").Value);
        result.Commands.Should().ContainSingle()
            .Which.Should().BeOfType<VerifyApplePlaybackReferenceCommand>();
        result.Events.Should().Contain(e => e is ApplePlaybackVerificationRequested);
    }

    [Fact]
    public async Task Given_An_Apple_Response_When_Handled_Then_It_Does_Not_Replace_Canonical_Metadata()
    {
        var appliedStore = new AppliedEnrichmentResponseStoreFake();
        var trackStore = new TrackEnrichmentWriteStoreFake();
        var handler = new ApplyEnrichmentResponseHandler(
            appliedStore,
            trackStore,
            new EnrichmentOrchestrator(new ActiveLookupWorkStoreFake()));

        await trackStore.ApplyAsync(
            MusicCatalogId.From("mc_track_1"),
            state => state.ApplyCanonicalMetadata(new SongMetadata("Canonical Song", "Canonical Artist", "isrc-1", "mbid-1", 123000)),
            CancellationToken.None);

        await handler.Handle(
            new EnrichmentResponse(
                CommandId.For("VerifyApplePlaybackReference:mc_track_1"),
                MusicCatalogId.From("mc_track_1"),
                ProviderName.Apple,
                LookupPriorityBand.High,
                new DateTimeOffset(2026, 6, 5, 12, 2, 0, TimeSpan.Zero),
                new SongMetadata("Apple Song", "Apple Artist", null, null, null),
                [new ExternalReference(ProviderName.Apple, new Uri("https://music.apple.com/track/1"), "apple-1", ReferenceConfidence.Verified)],
                CorrelationId.From("corr-2")));

        var state = trackStore.States["mc_track_1"];
        state.CanonicalMetadata.Should().NotBeNull();
        state.CanonicalMetadata!.Title.Should().Be("Canonical Song");
        state.Apple.Should().NotBeNull();
        state.Apple!.Confidence.Should().Be(ReferenceConfidence.Verified);
    }

    [Fact]
    public async Task Given_A_Duplicate_Response_When_Handled_Then_No_State_Is_Applied()
    {
        var appliedStore = new AppliedEnrichmentResponseStoreFake();
        var trackStore = new TrackEnrichmentWriteStoreFake();
        var handler = new ApplyEnrichmentResponseHandler(
            appliedStore,
            trackStore,
            new EnrichmentOrchestrator(new ActiveLookupWorkStoreFake()));
        var response = new EnrichmentResponse(
            CommandId.For("ResolveCanonicalMetadata:mc_track_1"),
            MusicCatalogId.From("mc_track_1"),
            ProviderName.MusicBrainz,
            LookupPriorityBand.High,
            new DateTimeOffset(2026, 6, 5, 12, 0, 0, TimeSpan.Zero),
            new SongMetadata("Song A", "Artist A", "isrc-1", "mbid-1", 123000),
            [],
            CorrelationId.From("corr-1"));

        var first = await handler.Handle(response);
        var duplicate = await handler.Handle(response);

        trackStore.States.Should().ContainSingle();
        first.Events.Should().NotBeEmpty();
        duplicate.Commands.Should().BeEmpty();
        duplicate.Events.Should().BeEmpty();
    }
}
