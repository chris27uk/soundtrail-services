using MusicResolver.Domain.ValueTypes;

namespace MusicResolver.Domain.Tracks;

public sealed record SearchResult(
    TrackTitle Title,
    ArtistName Artist,
    Isrc? Isrc,
    Mbid? Mbid,
    AppleId? AppleId,
    SpotifyId? SpotifyId,
    ConfidenceScore Confidence);
