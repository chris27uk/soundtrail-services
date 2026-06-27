using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Discovery.Commands;

namespace Soundtrail.Domain.Search;

public readonly record struct DiscoveryQueryKey(string StableValue) : IValueType
{
    public static DiscoveryQueryKey For(MusicSearchCriteria searchCriteria) =>
        new(
            searchCriteria.Kind switch
            {
                MusicSearchKind.UnifiedSearch => $"search:{searchCriteria.SearchTypes!.StableValue}:{searchCriteria.UnifiedQuery}",
                MusicSearchKind.Isrc => $"isrc:{searchCriteria.Isrc}",
                MusicSearchKind.TrackArtistAlbum => $"track-artist-album:{searchCriteria.NormalizedTitle}:{searchCriteria.NormalizedArtist}:{searchCriteria.NormalizedAlbum}",
                _ => throw new InvalidOperationException($"Unsupported music search kind '{searchCriteria.Kind}'.")
            });

    public static DiscoveryQueryKey For(MusicSeekOrSearchCriteria criteria) =>
        criteria.Match(
            onSearch: For,
            onSeek: seek => new DiscoveryQueryKey(
                seek switch
                {
                    { ArtistId: not null } => $"artist:{seek.ArtistId.Value.Value}",
                    { AlbumId: not null } => $"album:{seek.AlbumId.Value.Value}",
                    { TrackId: not null } => $"track:{seek.TrackId.Value.Value}",
                    _ => throw new InvalidOperationException("Known music catalog id must contain an artist id, album id or track id.")
                }));

    public static DiscoveryQueryKey For(KnownCatalogItem knownItem) =>
        new(
            knownItem switch
            {
                { ArtistId: not null } => $"artist:{knownItem.ArtistId.Value.Value}",
                { AlbumId: not null } => $"album:{knownItem.AlbumId.Value.Value}",
                { TrackId: not null } => $"track:{knownItem.TrackId.Value.Value}",
                _ => throw new InvalidOperationException("Known catalog item must contain an artist id, album id or track id.")
            });

    public static string StableValueFor(MusicSearchCriteria searchCriteria) => For(searchCriteria).StableValue;

    public static string StableValueFor(MusicSeekOrSearchCriteria criteria) => For(criteria).StableValue;

    public static string StableValueFor(KnownCatalogItem knownItem) => For(knownItem).StableValue;

    public static KnownCatalogItem ToKnownCatalogItem(string stableValue)
    {
        if (string.IsNullOrWhiteSpace(stableValue))
        {
            throw new ArgumentException("Stable value is required.", nameof(stableValue));
        }

        var parts = stableValue.Split(':', StringSplitOptions.None);
        return parts[0] switch
        {
            "artist" when parts.Length >= 2 => KnownCatalogItem.ForArtist(ArtistId.From(string.Join(':', parts.Skip(1)))),
            "album" when parts.Length >= 2 => KnownCatalogItem.ForAlbum(AlbumId.From(string.Join(':', parts.Skip(1)))),
            "track" when parts.Length >= 2 => KnownCatalogItem.ForTrack(TrackId.From(string.Join(':', parts.Skip(1)))),
            _ => throw new InvalidOperationException($"Unsupported known catalog item stable value '{stableValue}'.")
        };
    }

    public static MusicSearchCriteria ToMusicSearchCriteria(string stableValue)
    {
        if (string.IsNullOrWhiteSpace(stableValue))
        {
            throw new ArgumentException("Stable value is required.", nameof(stableValue));
        }

        var parts = stableValue.Split(':', StringSplitOptions.None);
        return parts[0] switch
        {
            "search" when parts.Length >= 3 => MusicSearchCriteria.ByQuery(
                string.Join(':', parts.Skip(2)),
                SearchTypesFilter.FromStableValue(parts[1])),
            "isrc" when parts.Length >= 2 => MusicSearchCriteria.ByIsrc(string.Join(':', parts.Skip(1))),
            "track-artist-album" when parts.Length >= 4 => MusicSearchCriteria.ByTrackArtistAlbum(parts[1], parts[2], parts[3]),
            _ => throw new InvalidOperationException($"Unsupported stable value '{stableValue}'.")
        };
    }

    public static MusicSeekOrSearchCriteria ToSearchOrSeekCriteria(string stableValue)
    {
        if (string.IsNullOrWhiteSpace(stableValue))
        {
            throw new ArgumentException("Stable value is required.", nameof(stableValue));
        }

        var parts = stableValue.Split(':', StringSplitOptions.None);
        return parts[0] switch
        {
            "search" or "isrc" or "track-artist-album" => MusicSeekOrSearchCriteria.FromSearch(ToMusicSearchCriteria(stableValue)),
            "artist" when parts.Length >= 2 => MusicSeekOrSearchCriteria.FromSeek(
                KnownMusicCatalogId.FromArtistId(ArtistId.From(string.Join(':', parts.Skip(1))))),
            "album" when parts.Length >= 2 => MusicSeekOrSearchCriteria.FromSeek(
                KnownMusicCatalogId.FromAlbumId(AlbumId.From(string.Join(':', parts.Skip(1))))),
            "track" when parts.Length >= 2 => MusicSeekOrSearchCriteria.FromSeek(
                KnownMusicCatalogId.FromTrackId(TrackId.From(string.Join(':', parts.Skip(1))))),
            _ => throw new InvalidOperationException($"Unsupported stable value '{stableValue}'.")
        };
    }
}
