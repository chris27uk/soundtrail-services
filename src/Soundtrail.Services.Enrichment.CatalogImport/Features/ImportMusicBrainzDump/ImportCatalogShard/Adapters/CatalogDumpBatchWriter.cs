using System.Security.Cryptography;
using System.Text;
using Raven.Client.Documents;
using Soundtrail.Adapters.CatalogProjection;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Aggregates;
using Soundtrail.Domain.Catalog.Albums;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Catalog.Events;
using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.ImportCatalogShard.Ports;

namespace Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.ImportCatalogShard.Adapters;

public sealed class CatalogDumpBatchWriter(
    IEventStreamRepository<ArtistId> artistRepository,
    IDocumentStore documentStore,
    ICommandBus commandBus) : ICatalogDumpBatchWriter
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
        var streamingLocationRequests = new List<TrackId>();

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

                        var trackToWrite = TrackForWrite(track, dumpObservedAt);
                        catalog.CatalogItemDiscovered(new CatalogItem.MusicTrack(trackToWrite));
                        pendingKeys.Add($"track:{track.TrackId.Value}");
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

            var (reloaded, _) = await ArtistCatalog.LoadAsync(artistRepository, artistId, cancellationToken);
            var projection = ArtistCatalogProjectionMaterializer.Build(artistId, reloaded.Events);
            readModels.AddRange(ArtistCatalogProjectionDocuments.CreateBrowseDocuments(projection));
            readModels.AddRange(ArtistCatalogProjectionDocuments.CreateSearchCandidateDocuments(projection, pendingKeys));

            foreach (var pendingKey in pendingKeys)
            {
                if (!pendingKey.StartsWith("track:", StringComparison.Ordinal))
                {
                    continue;
                }

                var trackIdValue = pendingKey["track:".Length..];
                var projectedTrack = projection.Tracks.FirstOrDefault(track =>
                    string.Equals(track.TrackId.Value, trackIdValue, StringComparison.Ordinal));
                if (projectedTrack is not null && projectedTrack.StreamingLocations.Length == 0)
                {
                    streamingLocationRequests.Add(projectedTrack.TrackId);
                }
            }
        }

        if (readModels.Count > 0)
        {
            await using var bulk = documentStore.BulkInsert();
            foreach (var (id, document) in DeduplicateById(readModels))
            {
                await bulk.StoreAsync(document, id);
            }
        }

        foreach (var trackId in streamingLocationRequests.Distinct())
        {
            await commandBus.SendAsync(
                new RequestKnownMusicDataMessage(
                    new CatalogItemOperation.StreamingLocationForTrack(trackId),
                    LookupPriorityBand.Low,
                    TrustLevel: 100,
                    RiskScore: 0,
                    dumpObservedAt)
                {
                    Id = MessageId.Deterministic(
                        "RequestKnownMusicData",
                        "bulk-import",
                        "streaming",
                        trackId.Value),
                    CorrelationId = CorrelationId.From($"musicbrainz-dump:{dumpObservedAt:O}")
                },
                cancellationToken);
        }
    }

    private static IEnumerable<(string Id, object Document)> DeduplicateById(
        IReadOnlyList<(string Id, object Document)> documents)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var pair in documents)
        {
            if (!seen.Add(pair.Id))
            {
                continue;
            }

            yield return pair;
        }
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

    private static Track TrackForWrite(Track track, DateTimeOffset dumpObservedAt)
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
}
