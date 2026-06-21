using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Domain.Events;
using Soundtrail.Services.Enrichment.Cdc.Features.PublishMusicTrackEvents;

namespace Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

internal sealed class PublishMusicTrackEventsHandlerTestEnvironment
{
    private PublishMusicTrackEventsHandlerTestEnvironment()
    {
        Publisher = new MusicTrackIntegrationEventPublisherFake();
        Handler = new PublishMusicTrackEventsHandler(Publisher);
    }

    public PublishMusicTrackEventsHandler Handler { get; }

    public MusicTrackIntegrationEventPublisherFake Publisher { get; }

    public static PublishMusicTrackEventsHandlerTestEnvironment Create() => new();

    public Task HandleAsync(params MusicTrackStoredEventRecordDto[] storedEvents) =>
        Handler.HandleAsync(storedEvents, CancellationToken.None);

    public static MusicTrackStoredEventRecordDto PlaybackReferencesResolutionRequired(
        string musicCatalogId,
        int version,
        string? isrc = "isrc-1",
        string? title = null,
        string? artist = null,
        string? album = null) =>
        new()
        {
            Id = MusicTrackStoredEventRecordDto.GetDocumentId(musicCatalogId, version),
            MusicCatalogId = musicCatalogId,
            Version = version,
            EventType = nameof(PlaybackReferencesResolutionRequired),
            PlaybackReferencesResolutionRequired = new PlaybackReferencesResolutionRequiredEventDataRecordDto(
                musicCatalogId,
                LookupPriorityBand.High.ToString(),
                "corr-1",
                ProviderName.MusicBrainz.Value,
                new DateTimeOffset(2026, 6, 18, 12, version, 0, TimeSpan.Zero),
                isrc,
                title,
                artist,
                album,
                "artist_1",
                "album_1"),
            OccurredAtUtc = new DateTimeOffset(2026, 6, 18, 12, version, 0, TimeSpan.Zero)
        };

    public static MusicTrackStoredEventRecordDto TrackDiscovered(
        string musicCatalogId,
        int version) =>
        new()
        {
            Id = MusicTrackStoredEventRecordDto.GetDocumentId(musicCatalogId, version),
            MusicCatalogId = musicCatalogId,
            Version = version,
            EventType = nameof(TrackDiscovered),
            TrackDiscovered = new TrackDiscoveredEventDataRecordDto(
                "Song A",
                "Artist A",
                123000,
                "isrc-1",
                "mbid-1",
                ProviderName.MusicBrainz.Value,
                new DateTimeOffset(2026, 6, 18, 12, version, 0, TimeSpan.Zero)),
            OccurredAtUtc = new DateTimeOffset(2026, 6, 18, 12, version, 0, TimeSpan.Zero)
        };
}
