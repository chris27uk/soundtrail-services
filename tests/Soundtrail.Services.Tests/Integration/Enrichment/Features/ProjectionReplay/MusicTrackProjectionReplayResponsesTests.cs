using FluentAssertions;
using Raven.Client.Documents.Session;
using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Domain.Events;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.EnrichmentResponse.Adapters;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.JustInTimeScheduling.Adapters.Documents;
using Soundtrail.Services.Tests.Integration.Api.Infrastructure;

namespace Soundtrail.Services.Tests.Integration.Enrichment.Features.ProjectionReplay;

[Collection(RavenEmbeddedCollection.Name)]
public sealed class MusicTrackProjectionReplayResponsesTests
{
    [Fact]
    public async Task Given_Replayable_Stored_Events_When_Projecting_Then_The_Track_Becomes_Playable()
    {
        using var raven = RavenEmbeddedTestDatabase.Create();
        using var session = raven.Store.OpenAsyncSession();
        var applier = new MusicTrackProjectionApplier();
        var musicCatalogId = MusicCatalogId.From("mc_track_1");
        var commandId = CommandId.For("ResolveCanonicalMetadata:mc_track_1");

        var storedEvents = new MusicTrackStoredEventRecordDto[]
        {
            new()
            {
                Id = MusicTrackStoredEventRecordDto.GetDocumentId(musicCatalogId.Value, 1),
                MusicCatalogId = musicCatalogId.Value,
                Version = 1,
                EventType = nameof(MinimalTrackInfoDiscovered),
                Data = System.Text.Json.JsonSerializer.Serialize(
                    new MinimalTrackInfoDiscoveredEventDataRecordDto(
                        "Song A",
                        "Artist A",
                        123000,
                        "isrc-1",
                        "mbid-1",
                        ProviderName.MusicBrainz.Value,
                        new DateTimeOffset(2026, 6, 6, 12, 0, 0, TimeSpan.Zero))),
                OccurredAtUtc = new DateTimeOffset(2026, 6, 6, 12, 0, 0, TimeSpan.Zero),
                CausationId = commandId.Value
            },
            new()
            {
                Id = MusicTrackStoredEventRecordDto.GetDocumentId(musicCatalogId.Value, 2),
                MusicCatalogId = musicCatalogId.Value,
                Version = 2,
                EventType = nameof(ProviderPlaybackReferenceResolved),
                Data = System.Text.Json.JsonSerializer.Serialize(
                    new ProviderPlaybackReferenceResolvedEventDataRecordDto(
                        ProviderName.AppleMusic.Value,
                        "apple-1",
                        "https://music.apple.com/us/song/song-a?i=apple-1",
                        ProviderName.Odesli.Value,
                        new DateTimeOffset(2026, 6, 6, 12, 1, 0, TimeSpan.Zero))),
                OccurredAtUtc = new DateTimeOffset(2026, 6, 6, 12, 1, 0, TimeSpan.Zero),
                CausationId = commandId.Value
            }
        };

        foreach (var storedEvent in storedEvents)
        {
            await applier.ApplyStoredEventAsync(storedEvent, session, CancellationToken.None);
        }

        await session.SaveChangesAsync(CancellationToken.None);

        using var verificationSession = raven.Store.OpenAsyncSession();
        var projection = await verificationSession.LoadAsync<RavenTrackRecordDto>(
            RavenTrackRecordDto.GetDocumentId(musicCatalogId.Value),
            CancellationToken.None);

        projection.Should().NotBeNull();
        projection!.Title.Should().Be("Song A");
        projection.Artist.Should().Be("Artist A");
        projection.AppleId.Should().Be("apple-1");
        projection.IsPlayable.Should().BeTrue();
        projection.ProjectionVersion.Should().Be(2);
    }

    [Fact]
    public async Task Given_An_Already_Applied_Event_Version_When_Projecting_Then_The_Duplicate_Is_Ignored()
    {
        using var raven = RavenEmbeddedTestDatabase.Create();
        var applier = new MusicTrackProjectionApplier();
        var storedEvent = new MusicTrackStoredEventRecordDto
        {
            Id = MusicTrackStoredEventRecordDto.GetDocumentId("mc_track_1", 1),
            MusicCatalogId = "mc_track_1",
            Version = 1,
            EventType = nameof(MinimalTrackInfoDiscovered),
            Data = System.Text.Json.JsonSerializer.Serialize(
                new MinimalTrackInfoDiscoveredEventDataRecordDto(
                    "Song A",
                    "Artist A",
                    123000,
                    "isrc-1",
                    "mbid-1",
                    ProviderName.MusicBrainz.Value,
                    new DateTimeOffset(2026, 6, 6, 12, 0, 0, TimeSpan.Zero))),
            OccurredAtUtc = new DateTimeOffset(2026, 6, 6, 12, 0, 0, TimeSpan.Zero)
        };

        using (var session = raven.Store.OpenAsyncSession())
        {
            await applier.ApplyStoredEventAsync(storedEvent, session, CancellationToken.None);
            await applier.ApplyStoredEventAsync(storedEvent, session, CancellationToken.None);
            await session.SaveChangesAsync(CancellationToken.None);
        }

        using var verificationSession = raven.Store.OpenAsyncSession();
        var projection = await verificationSession.LoadAsync<RavenTrackRecordDto>(
            RavenTrackRecordDto.GetDocumentId("mc_track_1"),
            CancellationToken.None);

        projection.Should().NotBeNull();
        projection!.ProjectionVersion.Should().Be(1);
    }
}
