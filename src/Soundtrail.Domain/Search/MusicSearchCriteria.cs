using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Search;
using Soundtrail.Contracts.Common;

namespace Soundtrail.Domain.Model;

public sealed record MusicSearchCriteria
{
    private MusicSearchCriteria(
        MusicSearchKind kind,
        string? query,
        string? isrc,
        string? title,
        string? artist,
        string? album,
        SearchTypesFilter? searchTypes)
    {
        Kind = kind;
        Query = query;
        Isrc = isrc;
        Title = title;
        Artist = artist;
        Album = album;
        SearchTypes = searchTypes;
    }

    public static MusicSearchCriteria ByQuery(string query, SearchTypesFilter? searchTypes = null) =>
        new(
            MusicSearchKind.UnifiedSearch,
            NormalizeRequired(query, nameof(query)),
            null,
            null,
            null,
            null,
            searchTypes ?? SearchTypesFilter.All);

    public static MusicSearchCriteria ByIsrc(string isrc) =>
        new(
            MusicSearchKind.Isrc,
            null,
            NormalizeCompactRequired(isrc, nameof(isrc)),
            null,
            null,
            null,
            null);

    public static MusicSearchCriteria ByTrackArtistAlbum(string title, string artist, string? album) =>
        new(
            MusicSearchKind.TrackArtistAlbum,
            null,
            null,
            RequireValue(title, nameof(title)),
            RequireValue(artist, nameof(artist)),
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
                withQuery(Query!);
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
            MusicSearchKind.UnifiedSearch => withQuery(Query!),
            MusicSearchKind.Isrc => withIsrcAction(Isrc!),
            MusicSearchKind.TrackArtistAlbum => withTitleAndArtist(Title!, Artist!, Album),
            _ => throw new InvalidOperationException($"Unsupported music search kind '{Kind}'.")
        };
    }

    public MusicSearchKind Kind { get; init; }
    public string? Query { get; init; }
    public string? Isrc { get; init; }
    public string? Title { get; init; }
    public string? Artist { get; init; }
    public string? Album { get; init; }
    public SearchTypesFilter? SearchTypes { get; init; }
    public string NormalizedTitle => MusicIdentityText.NormalizeFreeText(Title);
    public string NormalizedArtist => MusicIdentityText.NormalizeFreeText(Artist);
    public string NormalizedAlbum => MusicIdentityText.NormalizeFreeText(Album);

    private static string RequireValue(string value, string parameterName) =>
        string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("Value cannot be empty.", parameterName)
            : value;

    private static string NormalizeRequired(string value, string parameterName) =>
        string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("Value cannot be empty.", parameterName)
            : MusicIdentityText.NormalizeFreeText(value);

    private static string NormalizeCompactRequired(string value, string parameterName) =>
        string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("Value cannot be empty.", parameterName)
            : MusicIdentityText.NormalizeCompact(value);
}
