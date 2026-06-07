using FluentAssertions;
using Soundtrail.Contracts;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Execution;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.MusicTracks;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Prioritisation;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Search;
using Soundtrail.Services.Enrichment.Features.Execution.ApplyEnrichmentResponse;
using Soundtrail.Services.Tests.Enrichment.Unit.Infrastructure;

namespace Soundtrail.Services.Tests.Enrichment.Unit.Features.Execution.ApplyEnrichmentResponse;

public sealed class ApplyEnrichmentResponseHandlerTests
{
    [Fact]
    public async Task Given_A_MusicBrainz_Response_When_Handled_Then_A_Stream_Is_Created_Projection_Is_Updated_And_Provider_Resolution_Is_Scheduled()
    {
        var streamStore = new MusicTrackStreamStoreFake();
        var projectionStore = new MusicTrackProjectionStoreFake();
        var snapshotStore = new ProviderSnapshotStoreFake();
        var handler = new ApplyEnrichmentResponseHandler(
            streamStore,
            projectionStore,
            snapshotStore);

        var result = await handler.Handle(
            new EnrichmentResponse(
                CommandId.For("ResolveCanonicalMetadata:mc_track_1"),
                MusicCatalogId.From("mc_track_1"),
                ProviderName.MusicBrainz.Value,
                LookupPriorityBand.High,
                new DateTimeOffset(2026, 6, 5, 12, 0, 0, TimeSpan.Zero),
                new SongMetadata("Song A", "Artist A", "isrc-1", "mbid-1", 123000),
                [],
                CorrelationId.From("corr-1")));

        streamStore.Streams.Should().ContainKey("mc_track_1");
        streamStore.Streams["mc_track_1"].AppliedCommandIds.Should().Contain(CommandId.For("ResolveCanonicalMetadata:mc_track_1").Value);
        streamStore.Streams["mc_track_1"].Facts.Should().ContainItemsAssignableTo<MinimalTrackInfoDiscovered>();
        projectionStore.Projections.Should().ContainKey("mc_track_1");

        var projectedTrack = projectionStore.Projections["mc_track_1"];
        projectedTrack.CanonicalMetadata.Should().NotBeNull();
        projectedTrack.CanonicalMetadata!.Title.Should().Be("Song A");
        projectedTrack.Apple.Should().BeNull();
        snapshotStore.Snapshots.Should().ContainKey("mc_track_1:MusicBrainz");

        result.Facts.Should().ContainItemsAssignableTo<AppleMusicResolutionRequired>();
        result.Facts.Should().ContainItemsAssignableTo<YouTubeMusicResolutionRequired>();
    }

    [Fact]
    public async Task Given_An_Apple_Response_When_Handled_Then_The_Track_Becomes_Playable()
    {
        var streamStore = new MusicTrackStreamStoreFake();
        var projectionStore = new MusicTrackProjectionStoreFake();
        var snapshotStore = new ProviderSnapshotStoreFake();
        var seedResponse = new EnrichmentResponse(
            CommandId.For("ResolveCanonicalMetadata:mc_track_1"),
            MusicCatalogId.From("mc_track_1"),
            ProviderName.MusicBrainz.Value,
            LookupPriorityBand.High,
            new DateTimeOffset(2026, 6, 5, 12, 0, 0, TimeSpan.Zero),
            new SongMetadata("Canonical Song", "Canonical Artist", "isrc-1", "mbid-1", 123000),
            [],
            CorrelationId.From("corr-1"));
        var handler = new ApplyEnrichmentResponseHandler(
            streamStore,
            projectionStore,
            snapshotStore);

        await handler.Handle(seedResponse);
        var result = await handler.Handle(
            new EnrichmentResponse(
                CommandId.For("ResolveApplePlaybackReference:mc_track_1"),
                MusicCatalogId.From("mc_track_1"),
                ProviderName.AppleMusic.Value,
                LookupPriorityBand.High,
                new DateTimeOffset(2026, 6, 5, 12, 2, 0, TimeSpan.Zero),
                new SongMetadata("Apple Song", "Apple Artist", null, null, null),
                [new ExternalReference(ProviderName.AppleMusic, new Uri("https://music.apple.com/track/1"), "apple-1", ReferenceConfidence.Verified)],
                CorrelationId.From("corr-2")));

        var projectedTrack = projectionStore.Projections["mc_track_1"];
        projectedTrack.CanonicalMetadata.Should().NotBeNull();
        projectedTrack.CanonicalMetadata!.Title.Should().Be("Canonical Song");
        projectedTrack.Apple.Should().NotBeNull();
        projectedTrack.Apple!.Confidence.Should().Be(ReferenceConfidence.Verified);
        projectedTrack.IsPlayable.Should().BeTrue();
        result.Facts.Should().ContainSingle()
            .Which.Should().BeOfType<ProviderPlaybackReferenceResolved>();
    }

    [Fact]
    public async Task Given_A_Duplicate_Response_When_Handled_Then_No_Duplicate_Facts_Are_Appended()
    {
        var streamStore = new MusicTrackStreamStoreFake();
        var projectionStore = new MusicTrackProjectionStoreFake();
        var snapshotStore = new ProviderSnapshotStoreFake();
        var handler = new ApplyEnrichmentResponseHandler(
            streamStore,
            projectionStore,
            snapshotStore);
        var response = new EnrichmentResponse(
            CommandId.For("ResolveCanonicalMetadata:mc_track_1"),
            MusicCatalogId.From("mc_track_1"),
            ProviderName.MusicBrainz.Value,
            LookupPriorityBand.High,
            new DateTimeOffset(2026, 6, 5, 12, 0, 0, TimeSpan.Zero),
            new SongMetadata("Song A", "Artist A", "isrc-1", "mbid-1", 123000),
            [],
            CorrelationId.From("corr-1"));

        var first = await handler.Handle(response);
        var duplicate = await handler.Handle(response);

        streamStore.Streams.Should().ContainKey("mc_track_1");
        streamStore.Streams["mc_track_1"].AppliedCommandIds.Should().ContainSingle();
        streamStore.Streams["mc_track_1"].Facts.Should().NotBeEmpty();
        duplicate.Facts.Should().BeEmpty();
        first.Facts.Should().HaveCount(3);
    }
}
