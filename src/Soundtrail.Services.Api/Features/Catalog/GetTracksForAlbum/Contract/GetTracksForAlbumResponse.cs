using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Albums;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Services.Api.Features.Catalog.Shared.Contract;

namespace Soundtrail.Services.Api.Features.Catalog.GetTracksForAlbum.Contract;

public sealed record GetTracksForAlbumResponse(
    ArtistId ArtistId,
    AlbumId AlbumId,
    string AlbumTitle,
    GetTracksForAlbumTrackResponse[] Tracks,
    DiscoveryFeedbackResponse? Discovery = null);

public sealed record GetTracksForAlbumTrackResponse(
    TrackId TrackId,
    CatalogItemId MusicCatalogId,
    string Title,
    string ArtistName,
    int? DurationMs,
    string? Isrc,
    DateOnly? ReleaseDate,
    string? ArtworkUrl);
