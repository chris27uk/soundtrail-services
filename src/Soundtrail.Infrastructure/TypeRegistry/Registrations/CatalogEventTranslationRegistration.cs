using Soundtrail.Adapters.Registry;
using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Domain.Catalog.Albums;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Catalog.Events;
using Soundtrail.Domain.Catalog.Tracks;

namespace Soundtrail.Adapters.TypeRegistry.Registrations;

public sealed class CatalogEventTranslationRegistration : ITypeTranslationRegistration
{
    public void Register(TypeTranslationRegistry registry)
    {
        registry.RegisterStoredEventPair<ArtistDiscovered, ArtistDiscoveredEventDataRecordDto>(
            eventType: "artist-discovered",
            toDto: @event => new ArtistDiscoveredEventDataRecordDto(
                @event.Artist.Id.Value,
                @event.Artist.Name.Value,
                @event.Artist.Id.Value,
                "catalog",
                @event.ObservedAt),
            toDomainObject: dto => new ArtistDiscovered(
                new Artist
                {
                    Id = ArtistId.From(dto.ArtistId ?? throw new InvalidOperationException("Artist id is required.")),
                    Name = ArtistName.From(dto.ArtistName ?? throw new InvalidOperationException("Artist name is required."))
                },
                dto.ObservedAt),
            occurredAtUtc: @event => @event.ObservedAt);

        registry.RegisterStoredEventPair<AlbumDiscovered, AlbumDiscoveredEventDataRecordDto>(
            eventType: "album-discovered",
            toDto: @event => new AlbumDiscoveredEventDataRecordDto(
                @event.Album.AlbumId.StableValue,
                @event.Album.AlbumTitle,
                @event.Album.SourceAlbumId,
                @event.Album.ReleaseDate,
                "catalog",
                @event.ObservedAt),
            toDomainObject: dto => new AlbumDiscovered(
                new Album(
                    AlbumId.From(dto.AlbumId ?? throw new InvalidOperationException("Album id is required.")),
                    dto.AlbumTitle,
                    dto.SourceAlbumId,
                    dto.ReleaseDate,
                    artworkUrl: null,
                    updatedAt: dto.ObservedAt),
                dto.ObservedAt),
            occurredAtUtc: @event => @event.ObservedAt);

        registry.RegisterStoredEventPair<TrackDiscovered, TrackDiscoveredEventDataRecordDto>(
            eventType: "track-discovered",
            toDto: @event => new TrackDiscoveredEventDataRecordDto(
                @event.Track.TrackId.Value,
                @event.Track.Title,
                @event.Track.ArtistName,
                @event.Track.DurationMs,
                @event.Track.Isrc,
                @event.Track.Mbid,
                "catalog",
                @event.ObservedAt),
            toDomainObject: dto =>
            {
                var track = new Track(TrackId.From(dto.MusicCatalogId ?? throw new InvalidOperationException("Track id is required.")))
                {
                    Title = dto.Title,
                    ArtistName = dto.Artist,
                    DurationMs = dto.DurationMs,
                    Isrc = dto.Isrc,
                    Mbid = dto.Mbid,
                    UpdatedAt = dto.ObservedAt
                };

                return new TrackDiscovered(track, dto.ObservedAt);
            },
            occurredAtUtc: @event => @event.ObservedAt);
    }
}
