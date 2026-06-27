using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Catalog;

namespace Soundtrail.Domain.Search;

public sealed record MusicSearchCriteria
{
    private MusicSearchCriteria(
        MusicSearchKind kind,
        string? unifiedQuery,
        string? isrc,
        string? title,
        string? artist,
        string? album,
        SearchTypesFilter? searchTypes)
    {
        Kind = kind;
        UnifiedQuery = unifiedQuery;
        Isrc = isrc;
        Title = title;
        Artist = artist;
        Album = album;
        SearchTypes = searchTypes;
    }

    public static MusicSearchCriteria ByQuery(string query, SearchTypesFilter? searchTypes = null) =>
        new(
            MusicSearchKind.UnifiedSearch,
            string.IsNullOrWhiteSpace(query) ? throw new ArgumentException("Value cannot be empty.", nameof(query)) : MusicIdentityText.NormalizeFreeText(query),
            null,
            null,
            null,
            null,
            searchTypes ?? SearchTypesFilter.All);

    public static MusicSearchCriteria ByIsrc(string isrc) =>
        new(
            MusicSearchKind.Isrc,
            null,
            string.IsNullOrWhiteSpace(isrc)
                ? throw new ArgumentException("Value cannot be empty.", nameof(isrc))
                : MusicIdentityText.NormalizeCompact(isrc),
            null,
            null,
            null,
            null);

    public static MusicSearchCriteria ByTrackArtistAlbum(string title, string artist, string? album) =>
        new(
            MusicSearchKind.TrackArtistAlbum,
            null,
            null,
            string.IsNullOrWhiteSpace(title)
                ? throw new ArgumentException("Value cannot be empty.", nameof(title))
                : title,
            string.IsNullOrWhiteSpace(artist)
                ? throw new ArgumentException("Value cannot be empty.", nameof(artist))
                : artist,
            album,
            null);

    public void Match(
        Action<string> withQuery,
        Action<string, string, string?> withTitleAndArtist,
        Action<string> withIsrcAction)
    {
        switch (Kind)
        {
            case MusicSearchKind.UnifiedSearch:
                withQuery(UnifiedQuery!);
                return;
            case MusicSearchKind.Isrc:
                withIsrcAction(Isrc!);
                return;
            case MusicSearchKind.TrackArtistAlbum:
                withTitleAndArtist(Title!, Artist!, Album);
                return;
            default:
                throw new InvalidOperationException($"Unsupported music search kind '{Kind}'.");
        }
    }

    public void Match(Action<string, string, string?> withTitleAndArtist, Action<string> withIsrcAction)
    {
        Match(
            query => throw new InvalidOperationException($"Unified search '{query}' requires a handler that understands unified searches."),
            withTitleAndArtist,
            withIsrcAction);
    }

    public T Match<T>(Func<string, string, string?, T> withTitleAndArtist, Func<string, T> withIsrcAction)
    {
        return Match(
            query => throw new InvalidOperationException($"Unified search '{query}' requires a handler that understands unified searches."),
            withTitleAndArtist,
            withIsrcAction);
    }

    public T Match<T>(
        Func<string, T> withQuery,
        Func<string, string, string?, T> withTitleAndArtist,
        Func<string, T> withIsrcAction)
    {
        return Kind switch
        {
            MusicSearchKind.UnifiedSearch => withQuery(UnifiedQuery!),
            MusicSearchKind.Isrc => withIsrcAction(Isrc!),
            MusicSearchKind.TrackArtistAlbum => withTitleAndArtist(Title!, Artist!, Album),
            _ => throw new InvalidOperationException($"Unsupported music search kind '{Kind}'.")
        };
    }

    public MusicSearchKind Kind { get; init; }
    
    public string? UnifiedQuery { get; init; }
    
    public string? Isrc { get; init; }
    
    public string? Title { get; init; }
    
    public string? Artist { get; init; }
    
    public string? Album { get; init; }
    
    public SearchTypesFilter? SearchTypes { get; init; }
    
    public string NormalizedTitle => MusicIdentityText.NormalizeFreeText(Title);
    
    public string NormalizedArtist => MusicIdentityText.NormalizeFreeText(Artist);
    
    public string NormalizedAlbum => MusicIdentityText.NormalizeFreeText(Album);
}
