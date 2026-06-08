using FluentAssertions;
using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Events;
using Soundtrail.Domain.Model;
using Soundtrail.Domain.Responses;
using Soundtrail.Services.Enrichment.Features.Execution.ApplyEnrichmentResponse;
using Soundtrail.Services.Enrichment.Features.Orchestration;
using Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

namespace Soundtrail.Services.Tests.Unit.Enrichment.Features.Execution.ApplyEnrichmentResponse;

public sealed class ApplyEnrichmentResponseHandlerTests
{
    [Fact]
    public async Task Given_A_MusicBrainz_Response_When_Handled_Then_A_Stream_Is_Created()
    {
        var context = await HandleMusicBrainzResponse();

        context.StreamStore.Streams.Should().ContainKey("mc_track_1");
    }

    [Fact]
    public async Task Given_A_MusicBrainz_Response_When_Handled_Then_The_CommandId_Is_Recorded_On_The_Stream()
    {
        var context = await HandleMusicBrainzResponse();

        context.StreamStore.Streams["mc_track_1"].AppliedCommandIds.Should().Contain(CommandId.For("ResolveCanonicalMetadata:mc_track_1").Value);
    }

    [Fact]
    public async Task Given_A_MusicBrainz_Response_When_Handled_Then_A_MinimalTrackInfoDiscovered_Fact_Is_Appended()
    {
        var context = await HandleMusicBrainzResponse();

        context.StreamStore.Streams["mc_track_1"].Facts.Should().ContainItemsAssignableTo<MinimalTrackInfoDiscovered>();
    }

    [Fact]
    public async Task Given_A_MusicBrainz_Response_When_Handled_Then_A_Projection_Is_Created()
    {
        var context = await HandleMusicBrainzResponse();

        context.ProjectionStore.Projections.Should().ContainKey("mc_track_1");
    }

    [Fact]
    public async Task Given_A_MusicBrainz_Response_When_Handled_Then_The_Canonical_Title_Is_Projected()
    {
        var context = await HandleMusicBrainzResponse();

        context.ProjectionStore.Projections["mc_track_1"].CanonicalMetadata!.Title.Should().Be("Song A");
    }

    [Fact]
    public async Task Given_A_MusicBrainz_Response_When_Handled_Then_Apple_Remains_Unresolved()
    {
        var context = await HandleMusicBrainzResponse();

        context.ProjectionStore.Projections["mc_track_1"].Apple.Should().BeNull();
    }

    [Fact]
    public async Task Given_A_MusicBrainz_Response_When_Handled_Then_A_MusicBrainz_Snapshot_Is_Saved()
    {
        var context = await HandleMusicBrainzResponse();

        context.SnapshotStore.Snapshots.Should().ContainKey("mc_track_1:MusicBrainz");
    }

    [Fact]
    public async Task Given_A_MusicBrainz_Response_When_Handled_Then_An_AppleMusicResolutionRequired_Fact_Is_Returned()
    {
        var context = await HandleMusicBrainzResponse();

        context.Result.Facts.Should().ContainItemsAssignableTo<AppleMusicResolutionRequired>();
    }

    [Fact]
    public async Task Given_A_MusicBrainz_Response_When_Handled_Then_A_YouTubeMusicResolutionRequired_Fact_Is_Returned()
    {
        var context = await HandleMusicBrainzResponse();

        context.Result.Facts.Should().ContainItemsAssignableTo<YouTubeMusicResolutionRequired>();
    }

    [Fact]
    public async Task Given_An_Apple_Response_When_Handled_Then_The_Canonical_Title_Is_Preserved()
    {
        var context = await HandleAppleResponseAfterCanonicalResponse();

        context.ProjectionStore.Projections["mc_track_1"].CanonicalMetadata!.Title.Should().Be("Canonical Song");
    }

    [Fact]
    public async Task Given_An_Apple_Response_When_Handled_Then_An_Apple_Reference_Is_Stored()
    {
        var context = await HandleAppleResponseAfterCanonicalResponse();

        context.ProjectionStore.Projections["mc_track_1"].Apple.Should().NotBeNull();
    }

    [Fact]
    public async Task Given_An_Apple_Response_When_Handled_Then_The_Apple_Reference_Confidence_Is_Verified()
    {
        var context = await HandleAppleResponseAfterCanonicalResponse();

        context.ProjectionStore.Projections["mc_track_1"].Apple!.Confidence.Should().Be(ReferenceConfidence.Verified);
    }

    [Fact]
    public async Task Given_An_Apple_Response_When_Handled_Then_The_Track_Becomes_Playable()
    {
        var context = await HandleAppleResponseAfterCanonicalResponse();

        context.ProjectionStore.Projections["mc_track_1"].IsPlayable.Should().BeTrue();
    }

    [Fact]
    public async Task Given_An_Apple_Response_When_Handled_Then_A_ProviderPlaybackReferenceResolved_Fact_Is_Returned()
    {
        var context = await HandleAppleResponseAfterCanonicalResponse();

        context.Result.Facts.Should().ContainSingle().Which.Should().BeOfType<ProviderPlaybackReferenceResolved>();
    }

    [Fact]
    public async Task Given_A_Duplicate_Response_When_Handled_Then_The_Stream_Contains_A_Single_Applied_CommandId()
    {
        var context = await HandleDuplicateMusicBrainzResponse();

        context.StreamStore.Streams["mc_track_1"].AppliedCommandIds.Should().ContainSingle();
    }

    [Fact]
    public async Task Given_A_Duplicate_Response_When_Handled_Then_The_Original_Facts_Remain_On_The_Stream()
    {
        var context = await HandleDuplicateMusicBrainzResponse();

        context.StreamStore.Streams["mc_track_1"].Facts.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Given_A_Duplicate_Response_When_Handled_Then_No_New_Facts_Are_Returned()
    {
        var context = await HandleDuplicateMusicBrainzResponse();

        context.DuplicateResult.Facts.Should().BeEmpty();
    }

    [Fact]
    public async Task Given_A_Duplicate_Response_When_Handled_Then_The_First_Response_Returns_Three_Facts()
    {
        var context = await HandleDuplicateMusicBrainzResponse();

        context.FirstResult.Facts.Should().HaveCount(3);
    }

    private static async Task<MusicBrainzResponseContext> HandleMusicBrainzResponse()
    {
        var streamStore = new MusicTrackStreamStoreFake();
        var projectionStore = new MusicTrackProjectionStoreFake();
        var snapshotStore = new ProviderSnapshotStoreFake();
        var handler = new ApplyEnrichmentResponseHandler(streamStore, projectionStore, snapshotStore);

        var result = await handler.Handle(MusicBrainzResponse());

        return new MusicBrainzResponseContext(streamStore, projectionStore, snapshotStore, result);
    }

    private static async Task<AppleResponseContext> HandleAppleResponseAfterCanonicalResponse()
    {
        var streamStore = new MusicTrackStreamStoreFake();
        var projectionStore = new MusicTrackProjectionStoreFake();
        var snapshotStore = new ProviderSnapshotStoreFake();
        var handler = new ApplyEnrichmentResponseHandler(streamStore, projectionStore, snapshotStore);

        await handler.Handle(CanonicalResponse());
        var result = await handler.Handle(AppleResponse());

        return new AppleResponseContext(projectionStore, result);
    }

    private static async Task<DuplicateResponseContext> HandleDuplicateMusicBrainzResponse()
    {
        var streamStore = new MusicTrackStreamStoreFake();
        var projectionStore = new MusicTrackProjectionStoreFake();
        var snapshotStore = new ProviderSnapshotStoreFake();
        var handler = new ApplyEnrichmentResponseHandler(streamStore, projectionStore, snapshotStore);
        var response = MusicBrainzResponse();

        var first = await handler.Handle(response);
        var duplicate = await handler.Handle(response);

        return new DuplicateResponseContext(streamStore, first, duplicate);
    }

    private static EnrichmentResponse MusicBrainzResponse() =>
        new(
            CommandId.For("ResolveCanonicalMetadata:mc_track_1"),
            MusicCatalogId.From("mc_track_1"),
            ProviderName.MusicBrainz,
            LookupPriorityBand.High,
            new DateTimeOffset(2026, 6, 5, 12, 0, 0, TimeSpan.Zero),
            new SongMetadata("Song A", "Artist A", "isrc-1", "mbid-1", 123000),
            [],
            CorrelationId.From("corr-1"));

    private static EnrichmentResponse CanonicalResponse() =>
        new(
            CommandId.For("ResolveCanonicalMetadata:mc_track_1"),
            MusicCatalogId.From("mc_track_1"),
            ProviderName.MusicBrainz,
            LookupPriorityBand.High,
            new DateTimeOffset(2026, 6, 5, 12, 0, 0, TimeSpan.Zero),
            new SongMetadata("Canonical Song", "Canonical Artist", "isrc-1", "mbid-1", 123000),
            [],
            CorrelationId.From("corr-1"));

    private static EnrichmentResponse AppleResponse() =>
        new(
            CommandId.For("ResolveApplePlaybackReference:mc_track_1"),
            MusicCatalogId.From("mc_track_1"),
            ProviderName.AppleMusic,
            LookupPriorityBand.High,
            new DateTimeOffset(2026, 6, 5, 12, 2, 0, TimeSpan.Zero),
            new SongMetadata("Apple Song", "Apple Artist", null, null, null),
            [new ExternalReference(ProviderName.AppleMusic, new Uri("https://music.apple.com/track/1"), "apple-1", ReferenceConfidence.Verified)],
            CorrelationId.From("corr-2"));

    private sealed record MusicBrainzResponseContext(
        MusicTrackStreamStoreFake StreamStore,
        MusicTrackProjectionStoreFake ProjectionStore,
        ProviderSnapshotStoreFake SnapshotStore,
        EnrichmentOrchestrationResult Result);

    private sealed record AppleResponseContext(
        MusicTrackProjectionStoreFake ProjectionStore,
        EnrichmentOrchestrationResult Result);

    private sealed record DuplicateResponseContext(
        MusicTrackStreamStoreFake StreamStore,
        EnrichmentOrchestrationResult FirstResult,
        EnrichmentOrchestrationResult DuplicateResult);
}
