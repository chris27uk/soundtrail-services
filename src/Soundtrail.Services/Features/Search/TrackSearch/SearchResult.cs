using Soundtrail.Services.Features.Tracks;

namespace Soundtrail.Services.Features.Search.Models;

public sealed record SearchResult(
    TrackTitle Title,
    ArtistName Artist,
    Isrc? Isrc,
    Mbid? Mbid,
    AppleId? AppleId,
    SpotifyId? SpotifyId,
    ConfidenceScore Confidence);
