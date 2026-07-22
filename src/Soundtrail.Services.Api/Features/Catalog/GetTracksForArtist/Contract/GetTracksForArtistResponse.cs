using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Services.Api.Features.Catalog.Shared.Contract;

namespace Soundtrail.Services.Api.Features.Catalog.GetTracksForArtist.Contract;

public sealed record GetTracksForArtistResponse(
    ArtistId ArtistId,
    ArtistName ArtistName,
    GetTracksForArtistTrackResponse[] Tracks,
    DiscoveryFeedbackResponse? Discovery = null);

public sealed record GetTracksForArtistTrackResponse(
    TrackId TrackId,
    CatalogItemId MusicCatalogId,
    string Title,
    string ArtistName,
    string? AlbumTitle,
    int? DurationMs,
    string? Isrc,
    DateOnly? ReleaseDate,
    string? ArtworkUrl);
