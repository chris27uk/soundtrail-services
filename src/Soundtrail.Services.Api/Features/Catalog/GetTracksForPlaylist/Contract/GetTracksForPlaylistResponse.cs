using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Playlists;
using Soundtrail.Domain.Catalog.Tracks;

namespace Soundtrail.Services.Api.Features.Catalog.GetTracksForPlaylist.Contract;

public sealed record GetTracksForPlaylistResponse(
    PlaylistId PlaylistId,
    GetTracksForPlaylistTrackResponse[] Tracks);

public sealed record GetTracksForPlaylistTrackResponse(
    TrackId TrackId,
    CatalogItemId MusicCatalogId,
    string Title,
    string ArtistName,
    string? AlbumTitle,
    int? DurationMs,
    string? Isrc,
    DateOnly? ReleaseDate,
    string? ArtworkUrl);
