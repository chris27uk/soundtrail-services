using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Catalog.IntegrationEvents;
using Soundtrail.Domain.Commands;
using Soundtrail.Domain.Model;
using Soundtrail.Services.Public.Projector.Features.PublishMusicTrackEvents;

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

    public Task HandleAsync(params VersionedMusicTrackIntegrationEvent[] events) =>
        Handler.Handle(new PublishMusicTrackEventsCommand(events), CancellationToken.None);

    public static VersionedMusicTrackIntegrationEvent StreamingLocationsRequired(
        string musicCatalogId,
        int version,
        string? isrc = "isrc-1",
        string? title = null,
        string? artist = null,
        string? album = null) =>
        new(
            MusicCatalogId.From(musicCatalogId),
            version,
            new StreamingLocationsRequiredIntegrationEvent(
                MusicCatalogId.From(musicCatalogId),
                LookupPriorityBand.High,
                CorrelationId.From("corr-1"),
                ProviderName.MusicBrainz,
                new DateTimeOffset(2026, 6, 18, 12, version, 0, TimeSpan.Zero),
                !string.IsNullOrWhiteSpace(isrc)
                    ? MusicSearchCriteria.ByIsrc(isrc)
                    : MusicSearchCriteria.ByTrackArtistAlbum(
                        title ?? throw new InvalidOperationException("title is required when isrc is null"),
                        artist ?? throw new InvalidOperationException("artist is required when isrc is null"),
                        album),
                "artist_1",
                "album_1"));

    public static PublishMusicTrackEventsCommand EmptyCommand() => new([]);
}
