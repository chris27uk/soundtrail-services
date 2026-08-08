using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Services.Api.Shared.Contract;

namespace Soundtrail.Services.Api.Features.Catalog.GetTrack.Contract;

public sealed record GetTrackResponse(
    TrackId TrackId,
    string Title,
    string ArtistName,
    string? AlbumTitle,
    int? DurationMs,
    string? Isrc,
    DateOnly? ReleaseDate,
    string? ArtworkUrl,
    bool Playable,
    StreamingLocationResponse[] StreamingLocations,
    DiscoveryFeedbackResponse? Discovery = null);
