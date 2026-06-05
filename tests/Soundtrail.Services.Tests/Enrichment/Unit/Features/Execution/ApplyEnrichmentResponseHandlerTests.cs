using FluentAssertions;
using Soundtrail.Services.Enrichment.Features.Execution;
using Soundtrail.Services.Enrichment.Shared.Execution;
using Soundtrail.Services.Enrichment.Shared.Search;
using Soundtrail.Services.Shared;
using Soundtrail.Services.Tests.Enrichment.Unit.Infrastructure;

namespace Soundtrail.Services.Tests.Enrichment.Unit.Features.Execution;

public sealed class ApplyEnrichmentResponseHandlerTests
{
    [Fact]
    public async Task Given_A_MusicBrainz_Response_When_Handled_Then_Canonical_Metadata_And_Cross_Provider_References_Are_Applied()
    {
        var appliedStore = new AppliedEnrichmentResponseStoreFake();
        var trackStore = new TrackEnrichmentWriteStoreFake();
        var followUpScheduler = new FollowUpEnrichmentSchedulerFake();
        var handler = new ApplyEnrichmentResponseHandler(appliedStore, trackStore, followUpScheduler);

        await handler.Handle(
            new EnrichmentResponse(
                CommandId.For("MusicBrainz:mc_track_1"),
                MusicCatalogId.From("mc_track_1"),
                ProviderName.MusicBrainz,
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
        appliedStore.AppliedCommandIds.Should().Contain(CommandId.For("MusicBrainz:mc_track_1").Value);
        followUpScheduler.Scheduled.Should().ContainSingle();
    }

    [Fact]
    public async Task Given_An_Apple_Response_When_Handled_Then_It_Does_Not_Replace_Canonical_Metadata()
    {
        var appliedStore = new AppliedEnrichmentResponseStoreFake();
        var trackStore = new TrackEnrichmentWriteStoreFake();
        var followUpScheduler = new FollowUpEnrichmentSchedulerFake();
        var handler = new ApplyEnrichmentResponseHandler(appliedStore, trackStore, followUpScheduler);

        await trackStore.ApplyAsync(
            MusicCatalogId.From("mc_track_1"),
            state => state.ApplyCanonicalMetadata(new SongMetadata("Canonical Song", "Canonical Artist", "isrc-1", "mbid-1", 123000)),
            CancellationToken.None);

        await handler.Handle(
            new EnrichmentResponse(
                CommandId.For("Apple:mc_track_1"),
                MusicCatalogId.From("mc_track_1"),
                ProviderName.Apple,
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
        var followUpScheduler = new FollowUpEnrichmentSchedulerFake();
        var handler = new ApplyEnrichmentResponseHandler(appliedStore, trackStore, followUpScheduler);
        var response = new EnrichmentResponse(
            CommandId.For("MusicBrainz:mc_track_1"),
            MusicCatalogId.From("mc_track_1"),
            ProviderName.MusicBrainz,
            new SongMetadata("Song A", "Artist A", "isrc-1", "mbid-1", 123000),
            [],
            CorrelationId.From("corr-1"));

        await handler.Handle(response);
        await handler.Handle(response);

        trackStore.States.Should().ContainSingle();
        followUpScheduler.Scheduled.Should().ContainSingle();
    }
}
