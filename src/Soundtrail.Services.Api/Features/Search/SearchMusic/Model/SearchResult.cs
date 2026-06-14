using Soundtrail.Services.Api.Features.Search.Tracks;

namespace Soundtrail.Services.Api.Features.Search.TrackSearch;

public sealed record SearchResult(
    TrackTitle Title,
    ArtistName Artist,
    Isrc? Isrc,
    Mbid? Mbid,
    AppleId? AppleId,
    SpotifyId? SpotifyId,
    ConfidenceScore Confidence);
