namespace Soundtrail.Domain.Model;

public sealed record MusicSearchTerm
{
    private MusicSearchTerm(string? Isrc, string? Title, string? Artist, string? album = null)
    {
        this.Isrc = Isrc;
        this.Title = Title;
        this.Artist = Artist;
        this.Album = album;
    }

    public static MusicSearchTerm ByIsrc(string isrc) =>
        new(isrc, null, null);
    
    public static MusicSearchTerm ByTrackArtistAlbum(string title, string artist, string? album) =>
        new(null, title, artist, album);

    public void Match(Action<string, string, string?> withTitleAndArtist, Action<string> withIsrcAction)
    {
        if (!string.IsNullOrWhiteSpace(Isrc))
        {
            withIsrcAction(Isrc);
            return;
        }

        withTitleAndArtist(Title!, Artist!, Album);
    }
    
    public T Match<T>(Func<string, string, string?, T> withTitleAndArtist, Func<string, T> withIsrcAction)
    {
        if (!string.IsNullOrWhiteSpace(Isrc))
        {
            return withIsrcAction(Isrc);
        }

        return withTitleAndArtist(Title!, Artist!, Album);
    }

    public string? Isrc { get; init; }
    public string? Title { get; init; }
    public string? Artist { get; init; }
    public string? Album { get; init; }
}
