using Soundtrail.Services.Features.Tracks;

namespace Soundtrail.Services.Features.CatalogLookup.Models;

public sealed record CatalogLookupRequest(
    string? Isrc,
    string? AppleId,
    string? Mbid,
    string? SpotifyId,
    DurationMs? DurationMs)
{
    public static CatalogLookupRequest ByIsrc(string isrc) =>
        new(isrc.Trim().ToUpperInvariant(), null, null, null, null);

    public static CatalogLookupRequest Create(
        string? isrc,
        string? appleId,
        string? mbid,
        string? spotifyId,
        int? durationMs)
    {
        var request = new CatalogLookupRequest(
            NormalizeIsrc(isrc),
            Normalize(appleId),
            Normalize(mbid),
            Normalize(spotifyId),
            durationMs is null ? null : Tracks.DurationMs.From(durationMs.Value));

        if (!request.HasAnyIdentifier)
        {
            throw new ArgumentException(
                "At least one supported identifier must be supplied.");
        }

        return request;
    }

    public bool HasAnyIdentifier =>
        !string.IsNullOrWhiteSpace(Isrc) ||
        !string.IsNullOrWhiteSpace(AppleId) ||
        !string.IsNullOrWhiteSpace(Mbid) ||
        !string.IsNullOrWhiteSpace(SpotifyId);

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? NormalizeIsrc(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant();
}
