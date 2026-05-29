using Soundtrail.Services.Features.Tracks;

namespace Soundtrail.Services.Enrichment.Models;

public sealed record TrackMapping(
    Isrc? Isrc,
    Mbid? Mbid,
    AppleId? AppleMusicId,
    string? ITunesTrackId,
    ArtistName? Artist,
    TrackTitle? Title,
    DurationMs? Duration,
    string Source,
    double Confidence,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
