namespace Soundtrail.Services.Api.Features.SearchMusic.Tracks;

public sealed record Track(
    TrackTitle Title,
    ArtistName Artist,
    Isrc? Isrc,
    Mbid? Mbid,
    AppleId? AppleId,
    SpotifyId? SpotifyId,
    DurationMs? Duration);
