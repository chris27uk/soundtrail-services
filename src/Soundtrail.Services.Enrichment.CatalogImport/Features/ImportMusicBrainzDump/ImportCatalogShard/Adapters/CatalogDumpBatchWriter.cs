using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Raven.Client.Documents;
using Raven.Client.Documents.Session;
using Soundtrail.Adapters.CatalogProjection;
using Soundtrail.Adapters.EventSourcing;
using Soundtrail.Adapters.TypeRegistry;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Aggregates;
using Soundtrail.Domain.Catalog.Albums;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Catalog.Events;
using Soundtrail.Domain.Catalog.MusicBrainzDumpImport;
using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.ImportCatalogShard.Ports;

namespace Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.ImportCatalogShard.Adapters;

public sealed class CatalogDumpBatchWriter(
    IDocumentStore documentStore,
    ITypeRegistry typeRegistry,
    ICommandBus commandBus,
    IOptions<MusicBrainzDumpOptions> options,
    ILogger<CatalogDumpBatchWriter> logger) : ICatalogDumpBatchWriter
{
    private const string ArtistCatalogStreamName = "artist-catalog-stream";
    private const int ProjectionChunkSize = 200;
    private const int ProjectionLogInterval = 5_000;
    private const int RequestsPerArtistBudget = 4;

    public async Task<IReadOnlySet<ArtistId>> AppendEventsAsync(
        IReadOnlyList<CatalogDumpBatchItem> items,
        DateTimeOffset dumpObservedAt,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(items);
        if (items.Count == 0)
        {
            return EmptyArtistSet();
        }

        var flushItems = items
            .Where(static item => item is not TrackDumpBatchItem(var track) || !string.IsNullOrWhiteSpace(track.AlbumId))
            .ToArray();
        if (flushItems.Length == 0)
        {
            return EmptyArtistSet();
        }

        var touchedArtists = new HashSet<ArtistId>();
        var artistsPerSave = Math.Max(1, options.Value.EventAppendArtistsPerSaveChanges);
        var groups = flushItems
            .GroupBy(ArtistKeyFor, StringComparer.Ordinal)
            .ToArray();

        foreach (var chunk in groups.Chunk(artistsPerSave))
        {
            using var session = documentStore.OpenAsyncSession();
            session.Advanced.MaxNumberOfRequestsPerSession = Math.Max(
                session.Advanced.MaxNumberOfRequestsPerSession,
                (chunk.Length * RequestsPerArtistBudget) + 8);
            var artistRepository = CreateArtistRepository(session);
            var pendingSaves = 0;

            foreach (var group in chunk)
            {
                var artistId = ArtistId.From(group.Key);
                var (stream, catalog) = await ArtistCatalog.LoadAsync(artistRepository, artistId, cancellationToken);
                var pendingKeys = new List<string>();
                var emptyStream = stream.Events.Count == 0;

                foreach (var item in group)
                {
                    switch (item)
                    {
                        case ArtistDumpBatchItem(var artist):
                            if (!emptyStream && !ShouldWriteArtist(stream, dumpObservedAt))
                            {
                                break;
                            }

                            catalog.CatalogItemDiscovered(new CatalogItem.MusicArtist(artist));
                            pendingKeys.Add($"artist:{artist.Id.Value}");
                            break;

                        case AlbumDumpBatchItem(var album):
                            if (!emptyStream && !ShouldWriteAlbum(stream, album, dumpObservedAt))
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
                                (!emptyStream && !ShouldWriteTrack(stream, track, dumpObservedAt)))
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
                    ProjectionHint.BulkImport,
                    saveChanges: false);

                touchedArtists.Add(artistId);
                pendingSaves++;
            }

            if (pendingSaves > 0)
            {
                await session.SaveChangesAsync(cancellationToken);
            }
        }

        return touchedArtists;
    }

    public async Task ProjectArtistsAsync(
        IReadOnlySet<ArtistId> artistIds,
        DateTimeOffset dumpObservedAt,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(artistIds);
        if (artistIds.Count == 0)
        {
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        var projected = 0;
        var documentsWritten = 0;
        var readModels = new List<(string Id, object Document)>();
        var streamingLocationRequests = new List<TrackId>();

        foreach (var chunk in artistIds.Chunk(ProjectionChunkSize))
        {
            foreach (var artistId in chunk)
            {
                using var session = documentStore.OpenAsyncSession();
                var artistRepository = CreateArtistRepository(session);
                var (stream, _) = await ArtistCatalog.LoadAsync(artistRepository, artistId, cancellationToken);
                var projection = ArtistCatalogProjectionMaterializer.Build(artistId, stream.Events);
                readModels.AddRange(ArtistCatalogProjectionDocuments.CreateBrowseDocuments(projection));
                readModels.AddRange(
                    ArtistCatalogProjectionDocuments.CreateSearchCandidateDocumentsForFullProjection(projection));

                foreach (var track in projection.Tracks)
                {
                    if (track.StreamingLocations.Length == 0)
                    {
                        streamingLocationRequests.Add(track.TrackId);
                    }
                }

                projected++;
                if (projected % ProjectionLogInterval == 0)
                {
                    logger.LogInformation(
                        "MusicBrainz dump catalog projection progress: {Projected}/{Total} artists, {Docs} docs buffered, elapsed={ElapsedMs}ms.",
                        projected,
                        artistIds.Count,
                        readModels.Count,
                        stopwatch.ElapsedMilliseconds);
                }
            }

            if (readModels.Count > 0)
            {
                await using var bulk = documentStore.BulkInsert();
                foreach (var (id, document) in DeduplicateById(readModels))
                {
                    await bulk.StoreAsync(document, id);
                }

                documentsWritten += readModels.Count;
                readModels.Clear();
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

        logger.LogInformation(
            "MusicBrainz dump catalog projection finished: {Projected} artists, {Docs} docs written, {StreamingRequests} streaming requests, elapsed={ElapsedMs}ms.",
            projected,
            documentsWritten,
            streamingLocationRequests.Distinct().Count(),
            stopwatch.ElapsedMilliseconds);
    }

    private static HashSet<ArtistId> EmptyArtistSet() => [];

    private IEventStreamRepository<ArtistId> CreateArtistRepository(IAsyncDocumentSession session) =>
        new RavenEventStreamRepository<ArtistId>(session, typeRegistry, ArtistCatalogStreamName);

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
