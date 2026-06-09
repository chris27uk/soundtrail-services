namespace Soundtrail.Domain.Model;

public sealed record PlaybackReferenceLookupKey(
    PlaybackReferenceLookupMode Mode,
    string? Isrc,
    string? Title,
    string? Artist)
{
    public static PlaybackReferenceLookupKey ByIsrc(string isrc) =>
        new(PlaybackReferenceLookupMode.Isrc, isrc, null, null);

    public static PlaybackReferenceLookupKey ByTrackNameAndArtist(string title, string artist) =>
        new(PlaybackReferenceLookupMode.ByTrackNameAndArtist, null, title, artist);
}
