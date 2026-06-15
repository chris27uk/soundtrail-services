using FluentAssertions;
using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.IntegrationMessaging.Responses;
using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Contracts;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.EnrichmentResponse;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.EnrichmentResponse.Adapters;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.BacklogScheduling.Adapters;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.JustInTimeScheduling.Adapters;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.JustInTimeScheduling.Adapters.Documents;
using Soundtrail.Services.Tests.Integration.Api.Infrastructure;
using Soundtrail.Domain.Discovery;

namespace Soundtrail.Services.Tests.Integration.Enrichment.Features.ApplyEnrichmentResponse;

[Collection(RavenEmbeddedCollection.Name)]
public sealed class RavenEnrichmentResponseFlowResponsesTests
{
    [Fact]
    public async Task Given_Canonical_And_Playback_Responses_When_Applied_Then_The_Track_Becomes_Playable()
    {
        using var raven = RavenEmbeddedTestDatabase.Create();

        using (var seedSession = raven.Store.OpenAsyncSession())
        {
            await seedSession.StoreAsync(new RavenTrackRecordDto
            {
                Id = RavenTrackRecordDto.GetDocumentId("mc_track_1"),
                Title = "Rare Unknown Song",
                Artist = "Test Artist",
                AlbumTitle = "Rare Album",
                SearchText = RavenTrackRecordDto.BuildSearchText("Rare Unknown Song", "Test Artist"),
                CanonicalMetadata = new RavenSongMetadataRecordDto
                {
                    Title = "Rare Unknown Song",
                    Artist = "Test Artist"
                },
                IsPlayable = false
            });
            var trackingStore = new RavenCatalogSearchTrackingStore(raven.Store, seedSession);
            await trackingStore.UpsertAsync(
                new CatalogSearchTracking(
                    CatalogSearchCriteria.Search("track", "rare unknown song"),
                    MusicCatalogId.From("mc_track_1"),
                    new DateTimeOffset(2026, 6, 8, 12, 0, 0, TimeSpan.Zero)),
                CancellationToken.None);
            await seedSession.SaveChangesAsync();
        }

        using (var canonicalSession = raven.Store.OpenAsyncSession())
        {
            var listener = CreateListener(canonicalSession);
            await listener.Handle(CanonicalResponseDto(), null!);
            await canonicalSession.SaveChangesAsync();
        }

        using (var playbackSession = raven.Store.OpenAsyncSession())
        {
            var listener = CreateListener(playbackSession);
            await listener.Handle(PlaybackReferencesResponseDto(), null!);
            await playbackSession.SaveChangesAsync();
        }

        await ReplayProjectionsAsync(raven);

        using var verificationSession = raven.Store.OpenAsyncSession();
        var track = await verificationSession.LoadAsync<RavenTrackRecordDto>(
            RavenTrackRecordDto.GetDocumentId("mc_track_1"),
            CancellationToken.None);

        track.Should().NotBeNull();
        track!.IsPlayable.Should().BeTrue();
        track.AppleId.Should().Be("apple-track-1");
        track.Title.Should().Be("Rare Unknown Song");
        track.Artist.Should().Be("Test Artist");

        var status = await verificationSession.LoadAsync<CatalogSearchStatusRecordDto>(
            CatalogSearchStatusRecordDto.GetDocumentId(CatalogSearchCriteria.Search("track", "rare unknown song").Value),
            CancellationToken.None);
        status.Should().NotBeNull();
        status!.Status.Should().Be("Completed");
    }

    private static EnrichmentResponseListener CreateListener(Raven.Client.Documents.Session.IAsyncDocumentSession session) =>
        new(new ApplyEnrichmentResponseHandler(
            new RavenMusicTrackStreamStore(session),
            new RavenProviderSnapshotStore(session),
            new RavenCatalogSearchTrackingStore(session.Advanced.DocumentStore, session),
            new RavenUpsertCatalogSearchStatus(session.Advanced.DocumentStore)));

    private static async Task ReplayProjectionsAsync(RavenEmbeddedTestDatabase raven)
    {
        using var session = raven.Store.OpenAsyncSession();
        var events = await session.Advanced.AsyncDocumentQuery<MusicTrackStoredEventRecordDto>()
            .ToListAsync(CancellationToken.None);
        var applier = new MusicTrackProjectionApplier();

        foreach (var storedEvent in events.OrderBy(x => x.Version))
        {
            await applier.ApplyStoredEventAsync(storedEvent, session, CancellationToken.None);
        }

        await session.SaveChangesAsync(CancellationToken.None);
    }

    private static EnrichmentResponseDto CanonicalResponseDto() =>
        new(
            CommandId.For("ResolveCanonicalMetadata:mc_track_1").Value,
            "mc_track_1",
            ProviderName.MusicBrainz.Value,
            LookupPriorityBand.High,
            new DateTimeOffset(2026, 6, 8, 12, 0, 0, TimeSpan.Zero),
            new SongMetadataDto("Rare Unknown Song", "Test Artist", "isrc-1", "mbid-1", 123000),
            [],
            [],
            "corr-1");

    private static EnrichmentResponseDto PlaybackReferencesResponseDto() =>
        new(
            CommandId.For("ResolvePlaybackReferences:mc_track_1").Value,
            "mc_track_1",
            ProviderName.Odesli.Value,
            LookupPriorityBand.High,
            new DateTimeOffset(2026, 6, 8, 12, 2, 0, TimeSpan.Zero),
            null,
            [new ExternalReferenceDto(ProviderName.AppleMusic.Value, new Uri("https://music.apple.com/track/1"), "apple-track-1")],
            [],
            "corr-2");
}
