namespace Soundtrail.Domain.Model;

public abstract record CanonicalMusicMetadataLookup
{
    private CanonicalMusicMetadataLookup()
    {
    }

    public sealed record ByIsrc(string Isrc) : CanonicalMusicMetadataLookup;

    public sealed record ByTrackNameArtistAndAlbum(
        string TrackName,
        string ArtistName,
        string? AlbumName) : CanonicalMusicMetadataLookup;

    public static CanonicalMusicMetadataLookup FromIsrc(string isrc) => new ByIsrc(isrc);

    public static CanonicalMusicMetadataLookup FromTrackNameArtistAndAlbum(
        string trackName,
        string artistName,
        string? albumName) =>
        new ByTrackNameArtistAndAlbum(trackName, artistName, albumName);
}
