using System.Security.Cryptography;
using System.Text;
using Raven.Client.Documents;
using Soundtrail.Contracts.Persistence;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Aggregates;
using Soundtrail.Domain.Catalog.Albums;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Catalog.Events;
using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Domain.Common;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.ImportCatalogShard.Ports;

namespace Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.ImportCatalogShard.Adapters;

public sealed class CatalogDumpBatchWriter(
    IEventStreamRepository<ArtistId> artistRepository,
    IDocumentStore documentStore) : ICatalogDumpBatchWriter
{
    public async Task FlushAsync(
        IReadOnlyList<CatalogDumpBatchItem> items,
        DateTimeOffset dumpObservedAt,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(items);
        if (items.Count == 0)
        {
            return;
        }

        var flushItems = items
            .Where(static item => item is not TrackDumpBatchItem(var track) || !string.IsNullOrWhiteSpace(track.AlbumId))
            .ToArray();
        if (flushItems.Length == 0)
        {
            return;
        }

        var readModels = new List<(string Id, object Document)>();

        foreach (var group in flushItems.GroupBy(ArtistKeyFor, StringComparer.Ordinal))
        {
            var artistId = ArtistId.From(group.Key);
            var (stream, catalog) = await ArtistCatalog.LoadAsync(artistRepository, artistId, cancellationToken);
            var pendingKeys = new List<string>();

            foreach (var item in group)
            {
                switch (item)
                {
                    case ArtistDumpBatchItem(var artist):
                        if (!ShouldWriteArtist(stream, dumpObservedAt))
                        {
                            break;
                        }

                        catalog.CatalogItemDiscovered(new CatalogItem.MusicArtist(artist));
                        pendingKeys.Add($"artist:{artist.Id.Value}");
                        readModels.Add((
                            CatalogArtistRecordDto.GetDocumentId(artist.Id.Value),
                            CatalogDumpReadModels.Artist(artist, dumpObservedAt)));
                        break;

                    case AlbumDumpBatchItem(var album):
                        if (!ShouldWriteAlbum(stream, album, dumpObservedAt))
                        {
                            break;
                        }

                        var albumToWrite = new Album(
                            album.AlbumId,
                            album.AlbumTitle,
                            album.SourceSystemIds,
                            album.ReleaseDate,
                            album.ArtworkUrl,
                            dumpObservedAt);
                        catalog.CatalogItemDiscovered(new CatalogItem.MusicAlbum(albumToWrite));
                        pendingKeys.Add($"album:{album.AlbumId.StableValue}");
                        break;

                    case TrackDumpBatchItem(var track):
                        if (string.IsNullOrWhiteSpace(track.AlbumId) ||
                            !ShouldWriteTrack(stream, track, dumpObservedAt))
                        {
                            break;
                        }

                        var trackToWrite = CatalogDumpReadModels.TrackForWrite(track, dumpObservedAt);
                        catalog.CatalogItemDiscovered(new CatalogItem.MusicTrack(trackToWrite));
                        pendingKeys.Add($"track:{track.TrackId.Value}");
                        readModels.Add((
                            CatalogTrackRecordDto.GetDocumentId(track.TrackId.Value),
                            CatalogDumpReadModels.Track(artistId, trackToWrite, dumpObservedAt)));
                        break;
                }
            }

            if (pendingKeys.Count == 0)
            {
                continue;
            }

            var fingerprint = StableFingerprint(pendingKeys);
            await catalog.SaveAsync(
                artistRepository,
                stream,
                MessageId.For($"bulk-import:ArtistCatalog:{artistId.Value}:{dumpObservedAt:O}:{fingerprint}"),
                cancellationToken,
                ProjectionHint.BulkImport);

            foreach (var albumItem in group.OfType<AlbumDumpBatchItem>())
            {
                if (!pendingKeys.Contains($"album:{albumItem.Album.AlbumId.StableValue}", StringComparer.Ordinal))
                {
                    continue;
                }

                var albumToWrite = new Album(
                    albumItem.Album.AlbumId,
                    albumItem.Album.AlbumTitle,
                    albumItem.Album.SourceSystemIds,
                    albumItem.Album.ReleaseDate,
                    albumItem.Album.ArtworkUrl,
                    dumpObservedAt);
                await MergeArtistAlbumsReadModelAsync(
                    artistId,
                    albumToWrite,
                    dumpObservedAt,
                    readModels,
                    cancellationToken);
            }
        }

        if (readModels.Count == 0)
        {
            return;
        }

        await using var bulk = documentStore.BulkInsert();
        foreach (var (id, document) in readModels)
        {
            await bulk.StoreAsync(document, id);
        }
    }

    private async Task MergeArtistAlbumsReadModelAsync(
        ArtistId artistId,
        Album album,
        DateTimeOffset updatedAt,
        List<(string Id, object Document)> readModels,
        CancellationToken cancellationToken)
    {
        var documentId = CatalogArtistAlbumsRecordDto.GetDocumentId(artistId.Value);
        var existingInBatch = readModels
            .Where(static pair => pair.Document is CatalogArtistAlbumsRecordDto)
            .Select(static pair => (CatalogArtistAlbumsRecordDto)pair.Document)
            .FirstOrDefault(doc => string.Equals(doc.Id, documentId, StringComparison.Ordinal));

        CatalogArtistAlbumsRecordDto existing;
        if (existingInBatch is not null)
        {
            existing = existingInBatch;
            readModels.RemoveAll(pair => ReferenceEquals(pair.Document, existingInBatch));
        }
        else
        {
            using var session = documentStore.OpenAsyncSession();
            existing = await session.LoadAsync<CatalogArtistAlbumsRecordDto>(documentId, cancellationToken)
                ?? new CatalogArtistAlbumsRecordDto
                {
                    Id = documentId,
                    ArtistId = artistId.Value,
                    ArtistName = string.Empty,
                    Albums = []
                };
        }

        existing.Albums = existing.Albums
            .Where(item => !string.Equals(item.AlbumId, album.AlbumId.StableValue, StringComparison.Ordinal))
            .Append(
                new CatalogArtistAlbumRecordDto
                {
                    AlbumId = album.AlbumId.StableValue,
                    MusicCatalogId = album.AlbumId.StableValue,
                    AlbumTitle = album.AlbumTitle ?? string.Empty,
                    ReleaseDate = album.ReleaseDate,
                    ArtworkUrl = album.ArtworkUrl
                })
            .OrderBy(static item => item.ReleaseDate)
            .ThenBy(static item => item.AlbumTitle, StringComparer.Ordinal)
            .ToArray();
        existing.UpdatedAt = updatedAt;
        readModels.Add((documentId, existing));
    }

    private static bool ShouldWriteArtist(LoadedEventStream<ArtistId> stream, DateTimeOffset dumpObservedAt)
    {
        var existing = stream.Events.OfType<ArtistDiscovered>().LastOrDefault();
        return existing is null || existing.ObservedAt < dumpObservedAt;
    }

    private static bool ShouldWriteAlbum(
        LoadedEventStream<ArtistId> stream,
        Album album,
        DateTimeOffset dumpObservedAt)
    {
        var existing = stream.Events
            .OfType<AlbumDiscovered>()
            .LastOrDefault(@event => @event.Album.AlbumId.StableValue == album.AlbumId.StableValue);
        return existing is null || existing.ObservedAt < dumpObservedAt;
    }

    private static bool ShouldWriteTrack(
        LoadedEventStream<ArtistId> stream,
        Track track,
        DateTimeOffset dumpObservedAt)
    {
        var existing = stream.Events
            .OfType<TrackDiscovered>()
            .LastOrDefault(@event => @event.Track.TrackId.Value == track.TrackId.Value);
        return existing is null || existing.ObservedAt < dumpObservedAt;
    }

    private static string ArtistKeyFor(CatalogDumpBatchItem item) =>
        item switch
        {
            ArtistDumpBatchItem(var artist) => artist.Id.Value,
            AlbumDumpBatchItem(var album) => album.AlbumId.ArtistId,
            TrackDumpBatchItem(var track) => AlbumId.From(track.AlbumId!).ArtistId,
            _ => throw new InvalidOperationException($"Unsupported dump batch item '{item.GetType().Name}'.")
        };

    private static string StableFingerprint(IReadOnlyList<string> keys)
    {
        var joined = string.Join('\n', keys.OrderBy(static key => key, StringComparer.Ordinal));
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(joined));
        return Convert.ToHexString(hash.AsSpan(0, 8));
    }
}

internal static class CatalogDumpReadModels
{
    public static CatalogArtistRecordDto Artist(Artist artist, DateTimeOffset updatedAt) =>
        new()
        {
            Id = CatalogArtistRecordDto.GetDocumentId(artist.Id.Value),
            ArtistId = artist.Id.Value,
            Name = artist.Name.Value,
            NormalizedName = MusicIdentityText.NormalizeFreeText(artist.Name.Value),
            SearchText = artist.Name.Value,
            MusicBrainzArtistId = SourceSystemIdSet.MusicBrainzIdOrNull(artist.SourceSystemIds),
            AvailableProviders = [],
            TerminallyUnavailableProviders = [],
            ArtworkUrl = artist.ImageUrl,
            UpdatedAt = updatedAt
        };

    public static Track TrackForWrite(Track track, DateTimeOffset dumpObservedAt)
    {
        var trackToWrite = new Track(track.TrackId)
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
            UpdatedAt = dumpObservedAt
        };
        SourceSystemIdSet.UnionWith(trackToWrite.SourceSystemIds, track.SourceSystemIds);
        return trackToWrite;
    }

    public static CatalogTrackRecordDto Track(ArtistId artistId, Track track, DateTimeOffset updatedAt) =>
        new()
        {
            Id = CatalogTrackRecordDto.GetDocumentId(track.TrackId.Value),
            TrackId = track.TrackId.Value,
            MusicCatalogId = track.TrackId.Value,
            ArtistId = artistId.Value,
            Title = track.Title,
            ArtistName = track.ArtistName,
            AlbumTitle = track.AlbumTitle,
            AlbumId = track.AlbumId,
            DurationMs = track.DurationMs,
            Isrc = track.Isrc,
            ReleaseDate = track.ReleaseDate,
            ReleaseType = track.ReleaseType,
            ArtworkUrl = track.ArtworkUrl,
            StreamingLocations = [],
            UpdatedAt = updatedAt
        };
}
