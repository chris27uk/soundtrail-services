using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Albums;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;

namespace Soundtrail.Domain.Catalog.MusicBrainzDumpFreshness;

public sealed record DumpCatalogArtistSnapshot(
    string ArtistId,
    string? MusicBrainzArtistId,
    DateTimeOffset UpdatedAt);

public sealed record DumpCatalogAlbumSnapshot(
    string AlbumId,
    string AlbumTitle,
    DateOnly? ReleaseDate,
    string? ArtworkUrl);

public sealed record DumpCatalogAlbumsSnapshot(
    string ArtistId,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<DumpCatalogAlbumSnapshot> Albums);

public sealed record DumpCatalogTrackSnapshot(
    string TrackId,
    string ArtistId,
    string Title,
    string ArtistName,
    string? AlbumTitle,
    string? AlbumId,
    int? DurationMs,
    string? Isrc,
    DateOnly? ReleaseDate,
    string? ReleaseType,
    string? ArtworkUrl,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<DumpCatalogStreamingLocationSnapshot> StreamingLocations);

public sealed record DumpCatalogStreamingLocationSnapshot(
    string Provider,
    string? ExternalId,
    string Url);


public static class MusicBrainzDumpFreshnessPolicy
{
    public static bool IsWithinFreshWindow(
        DateTimeOffset updatedAt,
        DateTimeOffset utcNow,
        TimeSpan freshWithin) =>
        updatedAt >= utcNow - freshWithin;

    public static MusicBrainzDumpFreshnessDecision EvaluateArtistAlbums(
        DumpCatalogArtistSnapshot? artist,
        DumpCatalogAlbumsSnapshot? albums,
        DateTimeOffset utcNow,
        TimeSpan freshWithin)
    {
        if (artist is null ||
            string.IsNullOrWhiteSpace(artist.MusicBrainzArtistId) ||
            !IsWithinFreshWindow(artist.UpdatedAt, utcNow, freshWithin) ||
            albums is null ||
            albums.Albums.Count == 0 ||
            !IsWithinFreshWindow(albums.UpdatedAt, utcNow, freshWithin))
        {
            return MusicBrainzDumpFreshnessDecision.NeedsLiveLookup();
        }

        var artistId = ArtistId.From(artist.ArtistId);
        var entries = albums.Albums
            .Select(album => new CatalogDiscoveryEntry(
                artistId,
                new CatalogItem.MusicAlbum(
                    new Album(
                        AlbumId.From(album.AlbumId),
                        album.AlbumTitle,
                        SourceSystemIdSetFromAlbumId(album.AlbumId),
                        album.ReleaseDate,
                        album.ArtworkUrl,
                        albums.UpdatedAt))))
            .ToArray();

        return MusicBrainzDumpFreshnessDecision.UseExistingCatalog(entries);
    }

    public static MusicBrainzDumpFreshnessDecision EvaluateArtistTracks(
        DumpCatalogArtistSnapshot? artist,
        IReadOnlyList<DumpCatalogTrackSnapshot> tracks,
        DateTimeOffset utcNow,
        TimeSpan freshWithin)
    {
        if (artist is null ||
            string.IsNullOrWhiteSpace(artist.MusicBrainzArtistId) ||
            !IsWithinFreshWindow(artist.UpdatedAt, utcNow, freshWithin) ||
            tracks.Count == 0)
        {
            return MusicBrainzDumpFreshnessDecision.NeedsLiveLookup();
        }

        var entries = MapTracks(tracks);
        return entries.Count == 0
            ? MusicBrainzDumpFreshnessDecision.NeedsLiveLookup()
            : MusicBrainzDumpFreshnessDecision.UseExistingCatalog(entries);
    }

    public static MusicBrainzDumpFreshnessDecision EvaluateAlbumTracks(
        DumpCatalogArtistSnapshot? artist,
        DumpCatalogAlbumsSnapshot? albums,
        AlbumId albumId,
        IReadOnlyList<DumpCatalogTrackSnapshot> artistTracks,
        DateTimeOffset utcNow,
        TimeSpan freshWithin)
    {
        if (artist is null ||
            string.IsNullOrWhiteSpace(artist.MusicBrainzArtistId) ||
            !IsWithinFreshWindow(artist.UpdatedAt, utcNow, freshWithin) ||
            albums is null ||
            !IsWithinFreshWindow(albums.UpdatedAt, utcNow, freshWithin))
        {
            return MusicBrainzDumpFreshnessDecision.NeedsLiveLookup();
        }

        var album = albums.Albums.FirstOrDefault(item =>
            string.Equals(item.AlbumId, albumId.StableValue, StringComparison.Ordinal));
        if (album is null)
        {
            return MusicBrainzDumpFreshnessDecision.NeedsLiveLookup();
        }

        var matchingTracks = artistTracks
            .Where(track =>
                string.Equals(track.AlbumId, albumId.StableValue, StringComparison.Ordinal) ||
                string.Equals(track.AlbumTitle, album.AlbumTitle, StringComparison.Ordinal))
            .ToArray();

        if (matchingTracks.Length == 0)
        {
            return MusicBrainzDumpFreshnessDecision.NeedsLiveLookup();
        }

        var entries = MapTracks(matchingTracks);
        return entries.Count == 0
            ? MusicBrainzDumpFreshnessDecision.NeedsLiveLookup()
            : MusicBrainzDumpFreshnessDecision.UseExistingCatalog(entries);
    }

    private static IReadOnlyList<CatalogDiscoveryEntry> MapTracks(
        IReadOnlyList<DumpCatalogTrackSnapshot> tracks)
    {
        var entries = new List<CatalogDiscoveryEntry>(tracks.Count);
        foreach (var track in tracks)
        {
            TrackId trackId;
            try
            {
                trackId = TrackId.From(track.TrackId);
            }
            catch (ArgumentException)
            {
                continue;
            }

            var domainTrack = new Track(trackId)
            {
                Title = track.Title,
                ArtistName = track.ArtistName,
                AlbumTitle = track.AlbumTitle,
                AlbumId = track.AlbumId,
                DurationMs = track.DurationMs,
                Isrc = track.Isrc,
                ReleaseDate = track.ReleaseDate,
                ReleaseType = track.ReleaseType,
                ArtworkUrl = track.ArtworkUrl,
                UpdatedAt = track.UpdatedAt
            };

            foreach (var location in track.StreamingLocations)
            {
                if (!TryMapProvider(location.Provider, out var provider) ||
                    !Uri.TryCreate(location.Url, UriKind.Absolute, out var url))
                {
                    continue;
                }

                domainTrack.ProviderReferences[provider.Value] = new StreamingLocation(
                    provider,
                    location.ExternalId,
                    url,
                    LookupSource.Odesli,
                    track.UpdatedAt);
            }

            entries.Add(
                new CatalogDiscoveryEntry(
                    ArtistId.From(track.ArtistId),
                    new CatalogItem.MusicTrack(domainTrack)));
        }

        return entries.Count == 0
            ? Array.Empty<CatalogDiscoveryEntry>()
            : entries;
    }

    private static bool TryMapProvider(string provider, out ProviderName mapped)
    {
        switch (provider)
        {
            case "spotify":
            case "Spotify":
                mapped = ProviderName.Spotify;
                return true;
            case "appleMusic":
            case "AppleMusic":
                mapped = ProviderName.AppleMusic;
                return true;
            case "youtubeMusic":
            case "YoutubeMusic":
                mapped = ProviderName.YoutubeMusic;
                return true;
            default:
                mapped = default;
                return false;
        }
    }

    private static HashSet<SourceSystemId> SourceSystemIdSetFromAlbumId(string albumId)
    {
        try
        {
            var parsed = AlbumId.From(albumId);
            return SourceSystemIdSet.FromLegacyMusicBrainz(parsed.ArtistAlbumId);
        }
        catch (ArgumentException)
        {
            return [];
        }
    }
}
