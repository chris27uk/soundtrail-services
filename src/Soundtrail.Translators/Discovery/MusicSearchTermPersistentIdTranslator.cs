using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Discovery.Commands;
using Soundtrail.Domain.Search;

namespace Soundtrail.Translators.Discovery;

public static class MusicSearchTermPersistentIdTranslator
{
    public static string ToPersistentId(MusicSearchCriteria searchCriteria) =>
        searchCriteria.Kind switch
        {
            MusicSearchKind.UnifiedSearch => $"search:{searchCriteria.SearchTypes!.ToPersistentId()}:{searchCriteria.Query}",
            MusicSearchKind.Isrc => $"isrc:{searchCriteria.Isrc}",
            MusicSearchKind.TrackArtistAlbum => $"track-artist-album:{searchCriteria.NormalizedTitle}:{searchCriteria.NormalizedArtist}:{searchCriteria.NormalizedAlbum}",
            _ => throw new InvalidOperationException($"Unsupported music search kind '{searchCriteria.Kind}'.")
        };

    public static string ToPersistentId(MusicSeekOrSearchCriteria criteria) =>
        criteria.Match(
            onSearch: ToPersistentId,
            onSeek: seek => seek switch
            {
                { ArtistId: not null } => $"artist:{seek.ArtistId.Value.Value}",
                { AlbumId: not null } => $"album:{seek.AlbumId.Value.Value}",
                { TrackId: not null } => $"track:{seek.TrackId.Value.Value}",
                _ => throw new InvalidOperationException("Known music catalog id must contain an artist id, album id or track id.")
            });

    public static string ToPersistentId(KnownCatalogItem knownItem) =>
        knownItem switch
        {
            { ArtistId: not null } => $"artist:{knownItem.ArtistId.Value.Value}",
            { AlbumId: not null } => $"album:{knownItem.AlbumId.Value.Value}",
            { TrackId: not null } => $"track:{knownItem.TrackId.Value.Value}",
            _ => throw new InvalidOperationException("Known catalog item must contain an artist id, album id or track id.")
        };

    public static KnownCatalogItem ToKnownCatalogItem(string persistentId)
    {
        if (string.IsNullOrWhiteSpace(persistentId))
        {
            throw new ArgumentException("Persistent id is required.", nameof(persistentId));
        }

        var parts = persistentId.Split(':', StringSplitOptions.None);
        return parts[0] switch
        {
            "artist" when parts.Length >= 2 => KnownCatalogItem.ForArtist(ArtistId.From(string.Join(':', parts.Skip(1)))),
            "album" when parts.Length >= 2 => KnownCatalogItem.ForAlbum(AlbumId.From(string.Join(':', parts.Skip(1)))),
            "track" when parts.Length >= 2 => KnownCatalogItem.ForTrack(TrackId.From(string.Join(':', parts.Skip(1)))),
            _ => throw new InvalidOperationException($"Unsupported known catalog item persistent id '{persistentId}'.")
        };
    }

    public static MusicSearchCriteria ToDomainObject(string persistentId)
    {
        if (string.IsNullOrWhiteSpace(persistentId))
        {
            throw new ArgumentException("Persistent id is required.", nameof(persistentId));
        }

        var parts = persistentId.Split(':', StringSplitOptions.None);
        return parts[0] switch
        {
            "search" when parts.Length >= 3 => MusicSearchCriteria.ByQuery(
                string.Join(':', parts.Skip(2)),
                SearchTypesFilter.FromPersistentId(parts[1])),
            "isrc" when parts.Length >= 2 => MusicSearchCriteria.ByIsrc(string.Join(':', parts.Skip(1))),
            "track-artist-album" when parts.Length >= 4 => MusicSearchCriteria.ByTrackArtistAlbum(parts[1], parts[2], parts[3]),
            _ => throw new InvalidOperationException($"Unsupported persistent id '{persistentId}'.")
        };
    }

    public static MusicSeekOrSearchCriteria ToSearchOrSeekDomainObject(string persistentId)
    {
        if (string.IsNullOrWhiteSpace(persistentId))
        {
            throw new ArgumentException("Persistent id is required.", nameof(persistentId));
        }

        var parts = persistentId.Split(':', StringSplitOptions.None);
        return parts[0] switch
        {
            "search" or "isrc" or "track-artist-album" => MusicSeekOrSearchCriteria.FromSearch(ToDomainObject(persistentId)),
            "artist" when parts.Length >= 2 => MusicSeekOrSearchCriteria.FromSeek(
                KnownMusicCatalogId.FromArtistId(ArtistId.From(string.Join(':', parts.Skip(1))))),
            "album" when parts.Length >= 2 => MusicSeekOrSearchCriteria.FromSeek(
                KnownMusicCatalogId.FromAlbumId(AlbumId.From(string.Join(':', parts.Skip(1))))),
            "track" when parts.Length >= 2 => MusicSeekOrSearchCriteria.FromSeek(
                KnownMusicCatalogId.FromTrackId(TrackId.From(string.Join(':', parts.Skip(1))))),
            _ => throw new InvalidOperationException($"Unsupported persistent id '{persistentId}'.")
        };
    }
}
