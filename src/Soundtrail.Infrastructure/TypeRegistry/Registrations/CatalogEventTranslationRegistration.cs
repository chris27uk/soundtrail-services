using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Albums;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Catalog.Events;
using Soundtrail.Domain.Catalog.Playlists;
using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery.Events;

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
                @event.Hierarchy.ArtistId?.Value,
                @event.Hierarchy.AlbumId?.StableValue,
                @event.Track.TrackId.BaseKeyHigh,
                @event.Track.TrackId.BaseKeyLow,
                @event.Track.TrackId.SpecificKey,
                @event.Track.Title,
                @event.Track.ArtistName,
                @event.Track.AlbumTitle,
                @event.Track.DurationMs,
                @event.Track.Isrc,
                @event.Track.Mbid,
                @event.Track.ReleaseDate,
                @event.Track.ReleaseType,
                "catalog",
                @event.ObservedAt),
            toDomainObject: dto =>
            {
                var track = new Track(
                    TrackId.FromKeyParts(
                        dto.TrackIdBaseKeyHigh ?? throw new InvalidOperationException("Track base key high is required."),
                        dto.TrackIdBaseKeyLow ?? throw new InvalidOperationException("Track base key low is required."),
                        dto.TrackIdSpecificKey ?? throw new InvalidOperationException("Track specific key is required.")))
                {
                    Title = dto.Title,
                    ArtistName = dto.Artist,
                    AlbumId = dto.AlbumId,
                    AlbumTitle = dto.AlbumTitle,
                    DurationMs = dto.DurationMs,
                    Isrc = dto.Isrc,
                    Mbid = dto.Mbid,
                    ReleaseDate = dto.ReleaseDate,
                    ReleaseType = dto.ReleaseType,
                    UpdatedAt = dto.ObservedAt
                };

                return new TrackDiscovered(
                    track,
                    new CatalogTrackHierarchy(
                        string.IsNullOrWhiteSpace(dto.ArtistId) ? null : ArtistId.From(dto.ArtistId),
                        string.IsNullOrWhiteSpace(dto.AlbumId) ? null : AlbumId.From(dto.AlbumId)),
                    dto.ObservedAt);
            },
            occurredAtUtc: @event => @event.ObservedAt);

        registry.RegisterStoredEventPair<StreamingLocationDiscovered, ProviderReferenceDiscoveredEventDataRecordDto>(
            eventType: "streaming-location-discovered",
            toDto: @event => new ProviderReferenceDiscoveredEventDataRecordDto(
                @event.MusicCatalogId?.NormalisedIdentifier,
                @event.Hierarchy.ArtistId?.Value,
                @event.Provider.Value,
                @event.ExternalId,
                @event.Url.ToString(),
                @event.SourceProvider.Value,
                @event.ObservedAt),
            toDomainObject: dto => new StreamingLocationDiscovered(
                string.IsNullOrWhiteSpace(dto.MusicCatalogId) ? null : new CatalogItemId.Track(TrackId.From(dto.MusicCatalogId)),
                new CatalogTrackHierarchy(
                    string.IsNullOrWhiteSpace(dto.ArtistId) ? null : ArtistId.From(dto.ArtistId),
                    null),
                ProviderName.From(dto.Provider),
                dto.ExternalId,
                new Uri(dto.Url, UriKind.Absolute),
                LookupSource.From(dto.SourceProvider),
                dto.ObservedAt),
            occurredAtUtc: @event => @event.ObservedAt);

        registry.RegisterStoredEventPair<PlaylistTracksDiscovered, PlaylistTracksDiscoveredEventDataRecordDto>(
            eventType: "playlist-tracks-discovered",
            toDto: @event => new PlaylistTracksDiscoveredEventDataRecordDto(
                @event.PlaylistId.Value,
                @event.Tracks.Select(trackId => trackId.Value).ToArray(),
                @event.ObservedAt),
            toDomainObject: dto => new PlaylistTracksDiscovered(
                PlaylistId.FromPlaylistName(dto.PlaylistId),
                dto.TrackIds.Select(TrackId.From).ToArray(),
                dto.ObservedAtUtc),
            occurredAtUtc: @event => @event.ObservedAt);
    }
}
