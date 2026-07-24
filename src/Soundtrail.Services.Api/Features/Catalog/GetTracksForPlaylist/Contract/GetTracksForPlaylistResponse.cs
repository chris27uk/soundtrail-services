using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Playlists;
using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Services.Api.Features.Catalog.Shared.Contract;

namespace Soundtrail.Services.Api.Features.Catalog.GetTracksForPlaylist.Contract;

public sealed record GetTracksForPlaylistResponse(
    PlaylistId PlaylistId,
    GetTracksForPlaylistTrackResponse[] Tracks,
    DiscoveryFeedbackResponse? Discovery = null);

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
