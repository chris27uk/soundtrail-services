using Soundtrail.Domain.Catalog;

namespace Soundtrail.Services.Api.Features.GetTracksForArtist.Contract;

public sealed record GetTracksForArtistResponse(
    ArtistId ArtistId,
    ArtistName ArtistName,
    GetTracksForArtistTrackResponse[] Tracks);

public sealed record GetTracksForArtistTrackResponse(
    TrackId TrackId,
    MusicCatalogId MusicCatalogId,
    string Title,
    string ArtistName,
    string? AlbumTitle,
    int? DurationMs,
    string? Isrc,
    DateOnly? ReleaseDate,
    string? ArtworkUrl);
