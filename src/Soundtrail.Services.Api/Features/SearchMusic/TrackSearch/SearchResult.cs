using Soundtrail.Services.Api.Features.SearchMusic.Tracks;

namespace Soundtrail.Services.Api.Features.SearchMusic.TrackSearch;

public sealed record SearchResult(
    TrackTitle Title,
    ArtistName Artist,
    Isrc? Isrc,
    Mbid? Mbid,
    AppleId? AppleId,
    SpotifyId? SpotifyId,
    ConfidenceScore Confidence);
