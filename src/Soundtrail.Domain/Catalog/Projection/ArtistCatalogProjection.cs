using Soundtrail.Domain.Catalog.Albums;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Domain.Common;

namespace Soundtrail.Domain.Catalog.Projection;

public sealed record ArtistCatalogProjection(
    ArtistId ArtistId,
    string ArtistName,
    string? ArtworkUrl,
    string? MusicBrainzArtistId,
    DateTimeOffset UpdatedAt,
    ArtistCatalogAlbumProjection[] Albums,
    ArtistCatalogTrackProjection[] Tracks);

public sealed record ArtistCatalogAlbumProjection(
    AlbumId AlbumId,
    string AlbumTitle,
    string? SourceAlbumId,
    DateOnly? ReleaseDate,
    string? ArtworkUrl);

public sealed record ArtistCatalogTrackProjection(
    TrackId TrackId,
    string Title,
    string ArtistName,
    string? AlbumId,
    string? AlbumTitle,
    int? DurationMs,
    string? Isrc,
    DateOnly? ReleaseDate,
    string? ReleaseType,
    string? ArtworkUrl,
    ArtistCatalogStreamingLocationProjection[] StreamingLocations);

public sealed record ArtistCatalogStreamingLocationProjection(
    ProviderName Provider,
    string? ExternalId,
    string Url);
