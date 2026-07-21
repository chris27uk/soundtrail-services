using Soundtrail.Domain.Catalog.Albums;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Catalog.Tracks;

namespace Soundtrail.Services.Internal.Projector.Features.OnArtistCatalogChanged;

public sealed record ArtistCatalogReadModel(
    ArtistId ArtistId,
    string ArtistName,
    string? ArtworkUrl,
    DateTimeOffset UpdatedAt,
    ArtistCatalogAlbumReadModel[] Albums,
    ArtistCatalogTrackReadModel[] Tracks);

public sealed record ArtistCatalogAlbumReadModel(
    AlbumId AlbumId,
    string AlbumTitle,
    string? SourceAlbumId,
    DateOnly? ReleaseDate,
    string? ArtworkUrl);

public sealed record ArtistCatalogTrackReadModel(
    TrackId TrackId,
    string Title,
    string ArtistName,
    string? AlbumId,
    string? AlbumTitle,
    int? DurationMs,
    string? Isrc,
    DateOnly? ReleaseDate,
    string? ReleaseType,
    string? ArtworkUrl);
