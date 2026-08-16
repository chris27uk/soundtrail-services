using Soundtrail.Contracts.Persistence;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Artists;

namespace Soundtrail.Adapters.CatalogProjection;

public static class ArtistCatalogProjectionDocuments
{
    public static IReadOnlyList<(string Id, object Document)> CreateBrowseDocuments(ArtistCatalogProjection projection)
    {
        var documents = new List<(string Id, object Document)>();

        documents.Add((
            CatalogArtistRecordDto.GetDocumentId(projection.ArtistId.Value),
            new CatalogArtistRecordDto
            {
                Id = CatalogArtistRecordDto.GetDocumentId(projection.ArtistId.Value),
                ArtistId = projection.ArtistId.Value,
                Name = projection.ArtistName,
                NormalizedName = MusicIdentityText.NormalizeFreeText(projection.ArtistName),
                SearchText = projection.ArtistName,
                MusicBrainzArtistId = projection.MusicBrainzArtistId,
                AvailableProviders = [],
                TerminallyUnavailableProviders = [],
                ArtworkUrl = projection.ArtworkUrl,
                UpdatedAt = projection.UpdatedAt
            }));

        documents.Add((
            CatalogArtistAlbumsRecordDto.GetDocumentId(projection.ArtistId.Value),
            new CatalogArtistAlbumsRecordDto
            {
                Id = CatalogArtistAlbumsRecordDto.GetDocumentId(projection.ArtistId.Value),
                ArtistId = projection.ArtistId.Value,
                ArtistName = projection.ArtistName,
                Albums = projection.Albums
                    .OrderBy(static x => x.ReleaseDate)
                    .ThenBy(static x => x.AlbumTitle, StringComparer.Ordinal)
                    .Select(album => new CatalogArtistAlbumRecordDto
                    {
                        AlbumId = album.AlbumId.StableValue,
                        MusicCatalogId = album.AlbumId.StableValue,
                        AlbumTitle = album.AlbumTitle,
                        ReleaseDate = album.ReleaseDate,
                        ArtworkUrl = album.ArtworkUrl
                    })
                    .ToArray(),
                UpdatedAt = projection.UpdatedAt
            }));

        documents.Add((
            CatalogArtistTracksRecordDto.GetDocumentId(projection.ArtistId.Value),
            new CatalogArtistTracksRecordDto
            {
                Id = CatalogArtistTracksRecordDto.GetDocumentId(projection.ArtistId.Value),
                ArtistId = projection.ArtistId.Value,
                ArtistName = projection.ArtistName,
                Tracks = projection.Tracks
                    .OrderBy(static x => x.Title, StringComparer.Ordinal)
                    .Select(track => new CatalogArtistTrackRecordDto
                    {
                        TrackId = track.TrackId.Value,
                        MusicCatalogId = track.TrackId.Value,
                        Title = track.Title,
                        ArtistName = track.ArtistName,
                        AlbumTitle = track.AlbumTitle,
                        DurationMs = track.DurationMs,
                        Isrc = track.Isrc,
                        ReleaseDate = track.ReleaseDate,
                        ReleaseType = track.ReleaseType,
                        ArtworkUrl = track.ArtworkUrl,
                        StreamingLocations = ToStreamingLocationRecords(track.StreamingLocations)
                    })
                    .ToArray(),
                UpdatedAt = projection.UpdatedAt
            }));

        foreach (var album in projection.Albums)
        {
            documents.Add((
                CatalogAlbumRecordDto.GetDocumentId(album.AlbumId.StableValue),
                new CatalogAlbumRecordDto
                {
                    Id = CatalogAlbumRecordDto.GetDocumentId(album.AlbumId.StableValue),
                    ArtistId = projection.ArtistId.Value,
                    AlbumId = album.AlbumId.StableValue,
                    Name = album.AlbumTitle,
                    NormalizedName = MusicIdentityText.NormalizeFreeText(album.AlbumTitle),
                    ArtistName = projection.ArtistName,
                    SearchText = string.Join(
                        " ",
                        new[] { album.AlbumTitle, projection.ArtistName }.Where(static x => !string.IsNullOrWhiteSpace(x))),
                    MusicBrainzReleaseId = album.SourceAlbumId,
                    AvailableProviders = [],
                    TerminallyUnavailableProviders = [],
                    ArtworkUrl = album.ArtworkUrl,
                    ReleaseDate = album.ReleaseDate,
                    UpdatedAt = projection.UpdatedAt
                }));

            documents.Add((
                CatalogAlbumTracksRecordDto.GetDocumentId(album.AlbumId.StableValue),
                new CatalogAlbumTracksRecordDto
                {
                    Id = CatalogAlbumTracksRecordDto.GetDocumentId(album.AlbumId.StableValue),
                    ArtistId = projection.ArtistId.Value,
                    AlbumId = album.AlbumId.StableValue,
                    AlbumTitle = album.AlbumTitle,
                    Tracks = projection.Tracks
                        .Where(track => string.Equals(track.AlbumId, album.AlbumId.StableValue, StringComparison.Ordinal))
                        .OrderBy(static x => x.Title, StringComparer.Ordinal)
                        .Select(track => new CatalogAlbumTrackRecordDto
                        {
                            TrackId = track.TrackId.Value,
                            MusicCatalogId = track.TrackId.Value,
                            Title = track.Title,
                            ArtistName = track.ArtistName,
                            DurationMs = track.DurationMs,
                            Isrc = track.Isrc,
                            ReleaseDate = track.ReleaseDate,
                            ReleaseType = track.ReleaseType,
                            ArtworkUrl = track.ArtworkUrl,
                            StreamingLocations = ToStreamingLocationRecords(track.StreamingLocations)
                        })
                        .ToArray(),
                    UpdatedAt = projection.UpdatedAt
                }));
        }

        foreach (var track in projection.Tracks)
        {
            documents.Add((
                CatalogTrackRecordDto.GetDocumentId(track.TrackId.Value),
                new CatalogTrackRecordDto
                {
                    Id = CatalogTrackRecordDto.GetDocumentId(track.TrackId.Value),
                    TrackId = track.TrackId.Value,
                    MusicCatalogId = track.TrackId.Value,
                    ArtistId = projection.ArtistId.Value,
                    Title = track.Title,
                    ArtistName = track.ArtistName,
                    AlbumTitle = track.AlbumTitle,
                    AlbumId = track.AlbumId,
                    DurationMs = track.DurationMs,
                    Isrc = track.Isrc,
                    ReleaseDate = track.ReleaseDate,
                    ReleaseType = track.ReleaseType,
                    ArtworkUrl = track.ArtworkUrl,
                    StreamingLocations = ToStreamingLocationRecords(track.StreamingLocations),
                    UpdatedAt = projection.UpdatedAt
                }));
        }

        return documents;
    }

    /// <summary>
    /// Search candidates for dump flush dirty keys (<c>artist:</c>/<c>album:</c>/<c>track:</c>).
    /// </summary>
    public static IReadOnlyList<(string Id, object Document)> CreateSearchCandidateDocuments(
        ArtistCatalogProjection projection,
        IReadOnlyCollection<string> dirtyKeys)
    {
        var documents = new List<(string Id, object Document)>();
        var dirty = dirtyKeys as HashSet<string> ?? new HashSet<string>(dirtyKeys, StringComparer.Ordinal);

        if (dirty.Contains($"artist:{projection.ArtistId.Value}"))
        {
            documents.Add(SearchCandidate(
                projection.ArtistId.Value,
                "artist",
                projection.ArtistName,
                projection.ArtistName,
                null,
                null,
                projection.ArtworkUrl,
                projection.UpdatedAt));
        }

        foreach (var album in projection.Albums)
        {
            if (!dirty.Contains($"album:{album.AlbumId.StableValue}"))
            {
                continue;
            }

            documents.Add(SearchCandidate(
                album.AlbumId.StableValue,
                "album",
                album.AlbumTitle,
                album.AlbumTitle,
                null,
                album.AlbumTitle,
                album.ArtworkUrl,
                projection.UpdatedAt));
        }

        foreach (var track in projection.Tracks)
        {
            if (!dirty.Contains($"track:{track.TrackId.Value}"))
            {
                continue;
            }

            documents.Add(SearchCandidate(
                track.TrackId.Value,
                "track",
                $"{track.Title} {track.ArtistName}".Trim(),
                track.Title,
                track.ArtistName,
                track.AlbumTitle,
                track.ArtworkUrl,
                projection.UpdatedAt));
        }

        return documents;
    }

    private static (string Id, object Document) SearchCandidate(
        string catalogItemId,
        string candidateKind,
        string searchText,
        string title,
        string? artistName,
        string? albumTitle,
        string? artworkUrl,
        DateTimeOffset updatedAt)
    {
        var id = CatalogSearchCandidateRecordDto.GetDocumentId(catalogItemId);
        return (
            id,
            new CatalogSearchCandidateRecordDto
            {
                Id = id,
                CatalogItemId = catalogItemId,
                CandidateKind = candidateKind,
                SearchText = searchText,
                Title = title,
                ArtistName = artistName,
                AlbumTitle = albumTitle,
                ArtworkUrl = artworkUrl,
                UpdatedAt = updatedAt
            });
    }

    private static CatalogStreamingLocationRecordDto[] ToStreamingLocationRecords(
        IEnumerable<ArtistCatalogStreamingLocationProjection> streamingLocations) =>
        streamingLocations
            .Select(static location => new CatalogStreamingLocationRecordDto
            {
                Provider = location.Provider.StableValue,
                ExternalId = location.ExternalId,
                Url = location.Url
            })
            .ToArray();
}
