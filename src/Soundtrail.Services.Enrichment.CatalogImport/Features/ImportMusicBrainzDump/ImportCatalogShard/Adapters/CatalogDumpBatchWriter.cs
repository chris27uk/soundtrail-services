using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Raven.Client.Documents;
using Raven.Client.Documents.Session;
using Soundtrail.Adapters.CatalogProjection;
using Soundtrail.Adapters.EventSourcing;
using Soundtrail.Adapters.TypeRegistry;
using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Contracts.Persistence;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Aggregates;
using Soundtrail.Domain.Catalog.Albums;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Catalog.Events;
using Soundtrail.Domain.Catalog.MusicBrainzDumpImport;
using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Domain.Catalog.Projection;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard.Ports;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.ImportCatalogShard.Ports;

namespace Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.ImportCatalogShard.Adapters;

public sealed class CatalogDumpBatchWriter(
    IDocumentStore documentStore,
    ITypeRegistry typeRegistry,
    ICommandBus commandBus,
    IOptions<MusicBrainzDumpOptions> options,
    IArtistShardPartitioner partitioner,
    ILogger<CatalogDumpBatchWriter> logger) : ICatalogDumpBatchWriter
{
    private const string ArtistCatalogStreamName = "artist-catalog-stream";
    private const int ProjectionLogInterval = 5_000;
    private const int RequestsPerArtistBudget = 4;

    /// <summary>
    /// Process-local cache of last projected stream versions (shards are artist-partitioned).
    /// </summary>
    private static readonly ConcurrentDictionary<string, int> ProjectedVersionCache = new(StringComparer.Ordinal);

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
        var skippedUnchanged = 0;
        var documentsWritten = 0;
        var streamingLocationRequests = new ConcurrentBag<TrackId>();
        var parallelism = options.Value.ProjectionMaxDegreeOfParallelism;
        if (parallelism < 1)
        {
            parallelism = Environment.ProcessorCount;
        }

        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = parallelism,
            CancellationToken = cancellationToken
        };

        var writeIndividualTrackAndSearchDocs = options.Value.WriteIndividualTrackAndSearchDocsOnProjection;
        var projectionChunkSize = Math.Max(1, options.Value.ProjectionArtistsPerBulkInsert);
        foreach (var chunk in artistIds.Chunk(projectionChunkSize))
        {
            var toProject = await ArtistsNeedingProjectionAsync(chunk, cancellationToken);
            skippedUnchanged += chunk.Length - toProject.Count;
            if (toProject.Count == 0)
            {
                continue;
            }

            var readModels = new ConcurrentBag<(string Id, object Document)>();
            await Parallel.ForEachAsync(
                toProject,
                parallelOptions,
                async (artistId, token) =>
                {
                    using var session = documentStore.OpenAsyncSession();
                    var (streamVersion, projection) = await LoadProjectionAsync(session, artistId, token);

                    foreach (var document in ArtistCatalogProjectionDocuments.CreateBrowseDocuments(
                                 projection,
                                 includeIndividualTrackDocuments: writeIndividualTrackAndSearchDocs))
                    {
                        if (document.Document is CatalogArtistRecordDto artistDto)
                        {
                            artistDto.ProjectedStreamVersion = streamVersion;
                            ProjectedVersionCache[artistId.Value] = streamVersion;
                        }

                        readModels.Add(document);
                    }

                    // Search candidates are required for API search; projector CDC skips bulk-import.
                    // Per-track CatalogTrackRecordDto docs remain gated by the flag for Raven size.
                    foreach (var document in ArtistCatalogProjectionDocuments
                                 .CreateSearchCandidateDocumentsForFullProjection(projection))
                    {
                        readModels.Add(document);
                    }

                    var snapshot = ArtistCatalogProjectionSnapshotMapper.ToDto(projection, streamVersion);
                    readModels.Add((snapshot.Id, snapshot));

                    if (options.Value.EnqueueStreamingLocationLookupsOnProjection)
                    {
                        foreach (var track in projection.Tracks)
                        {
                            if (track.StreamingLocations.Length == 0)
                            {
                                streamingLocationRequests.Add(track.TrackId);
                            }
                        }
                    }

                    var done = Interlocked.Increment(ref projected);
                    if (done % ProjectionLogInterval == 0)
                    {
                        logger.LogInformation(
                            "MusicBrainz dump catalog projection progress: {Projected}/{Total} artists, {Skipped} unchanged, {Docs} docs buffered, elapsed={ElapsedMs}ms.",
                            done,
                            artistIds.Count,
                            skippedUnchanged,
                            readModels.Count,
                            stopwatch.ElapsedMilliseconds);
                    }
                });

            var buffered = readModels.ToArray();
            if (buffered.Length > 0)
            {
                await using var bulk = documentStore.BulkInsert();
                foreach (var (id, document) in DeduplicateById(buffered))
                {
                    await bulk.StoreAsync(document, id);
                }

                documentsWritten += buffered.Length;
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
            "MusicBrainz dump catalog projection finished: {Projected} artists, {Skipped} unchanged, {Docs} docs written, {StreamingRequests} streaming requests, elapsed={ElapsedMs}ms.",
            projected,
            skippedUnchanged,
            documentsWritten,
            streamingLocationRequests.Distinct().Count(),
            stopwatch.ElapsedMilliseconds);
    }

    private async Task<(int StreamVersion, ArtistCatalogProjection Projection)> LoadProjectionAsync(
        IAsyncDocumentSession session,
        ArtistId artistId,
        CancellationToken cancellationToken)
    {
        var metadataId = $"{ArtistCatalogStreamName}-streams/{artistId.Value}";
        var snapshotId = ArtistCatalogProjectionSnapshotDto.GetDocumentId(artistId.Value);
        var metadata = await session.LoadAsync<RavenEventStreamMetadataRecord>(metadataId, cancellationToken);
        var snapshotDto = await session.LoadAsync<ArtistCatalogProjectionSnapshotDto>(snapshotId, cancellationToken);
        var streamVersion = metadata?.Version ?? 0;

        if (snapshotDto is not null && snapshotDto.StreamVersion == streamVersion && streamVersion > 0)
        {
            return (streamVersion, ArtistCatalogProjectionSnapshotMapper.ToProjection(snapshotDto));
        }

        if (snapshotDto is not null && snapshotDto.StreamVersion > 0 && snapshotDto.StreamVersion < streamVersion)
        {
            var prior = ArtistCatalogProjectionSnapshotMapper.ToProjection(snapshotDto);
            var newEvents = await LoadEventsAfterAsync(session, artistId, snapshotDto.StreamVersion, cancellationToken);
            return (streamVersion, ArtistCatalogProjectionMaterializer.Build(artistId, prior, newEvents));
        }

        var artistRepository = CreateArtistRepository(session);
        var (stream, _) = await ArtistCatalog.LoadAsync(artistRepository, artistId, cancellationToken);
        return (stream.Version, ArtistCatalogProjectionMaterializer.Build(artistId, stream.Events));
    }

    private async Task<IReadOnlyList<IDomainEvent>> LoadEventsAfterAsync(
        IAsyncDocumentSession session,
        ArtistId artistId,
        int afterVersion,
        CancellationToken cancellationToken)
    {
        var prefix = $"{ArtistCatalogStreamName}-events/{artistId.Value}/";
        const int pageSize = 1_024;
        var storedEvents = new List<RavenStoredEventRecord>();
        string? lastId = null;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var page = (await session.Advanced.LoadStartingWithAsync<RavenStoredEventRecord>(
                prefix,
                start: 0,
                pageSize: pageSize,
                startAfter: lastId,
                token: cancellationToken)).ToArray();
            if (page.Length == 0)
            {
                break;
            }

            storedEvents.AddRange(page);
            lastId = page[^1].Id;
            if (page.Length < pageSize)
            {
                break;
            }
        }

        return storedEvents
            .Where(stored => stored.Version > afterVersion)
            .OrderBy(stored => stored.Version)
            .Select(ToDomainEvent)
            .ToArray();
    }

    private IDomainEvent ToDomainEvent(RavenStoredEventRecord storedEvent)
    {
        if (storedEvent.Body is null)
        {
            throw new InvalidOperationException($"Stored event '{storedEvent.Id}' is missing a body.");
        }

        return (IDomainEvent)typeRegistry.ToDomainObject(storedEvent.Body);
    }

    private async Task<List<ArtistId>> ArtistsNeedingProjectionAsync(
        IReadOnlyList<ArtistId> artistIds,
        CancellationToken cancellationToken)
    {
        if (artistIds.Count == 0)
        {
            return [];
        }

        using var session = documentStore.OpenAsyncSession();
        var documentIds = artistIds.Select(id => CatalogArtistRecordDto.GetDocumentId(id.Value)).ToArray();
        var metadataIds = artistIds
            .Select(id => $"{ArtistCatalogStreamName}-streams/{id.Value}")
            .ToArray();
        var existing = await session.LoadAsync<CatalogArtistRecordDto>(documentIds, cancellationToken);
        var metadata = await session.LoadAsync<RavenEventStreamMetadataRecord>(metadataIds, cancellationToken);

        var needed = new List<ArtistId>(artistIds.Count);
        foreach (var artistId in artistIds)
        {
            var documentId = CatalogArtistRecordDto.GetDocumentId(artistId.Value);
            var metadataId = $"{ArtistCatalogStreamName}-streams/{artistId.Value}";
            existing.TryGetValue(documentId, out var dto);
            metadata.TryGetValue(metadataId, out var streamMetadata);

            var projectedVersion = dto?.ProjectedStreamVersion;
            if (projectedVersion is null &&
                ProjectedVersionCache.TryGetValue(artistId.Value, out var cachedVersion))
            {
                projectedVersion = cachedVersion;
            }

            if (streamMetadata is not null &&
                projectedVersion is int version &&
                version >= streamMetadata.Version)
            {
                ProjectedVersionCache[artistId.Value] = streamMetadata.Version;
                continue;
            }

            needed.Add(artistId);
        }

        return needed;
    }

    public async IAsyncEnumerable<ArtistId> EnumerateArtistsNeedingProjectionForShardAsync(
        int shardId,
        int shardCount,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(shardId);
        ArgumentOutOfRangeException.ThrowIfLessThan(shardCount, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(shardId, shardCount);

        const int pageSize = 1_024;
        const int filterBatchSize = 2_000;
        var prefix = $"{ArtistCatalogStreamName}-streams/";
        string? lastId = null;
        var batch = new List<(ArtistId ArtistId, int StreamVersion)>(filterBatchSize);

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var session = documentStore.OpenAsyncSession();
            // Prefer startAfter (O(page)) over numeric start skips — those get slower as the
            // cursor advances through multi-million stream-metadata collections.
            var page = (await session.Advanced.LoadStartingWithAsync<RavenEventStreamMetadataRecord>(
                prefix,
                start: 0,
                pageSize: pageSize,
                startAfter: lastId,
                token: cancellationToken)).ToArray();
            if (page.Length == 0)
            {
                break;
            }

            foreach (var metadata in page)
            {
                lastId = metadata.Id;
                var artistKey = metadata.StreamId;
                if (string.IsNullOrWhiteSpace(artistKey) ||
                    partitioner.ShardIdFor(artistKey, shardCount) != shardId ||
                    metadata.Version <= 0)
                {
                    continue;
                }

                batch.Add((ArtistId.From(artistKey), metadata.Version));
                if (batch.Count < filterBatchSize)
                {
                    continue;
                }

                foreach (var artistId in await FilterNeedingFromVersionsAsync(batch, cancellationToken))
                {
                    yield return artistId;
                }

                batch.Clear();
            }

            if (page.Length < pageSize)
            {
                break;
            }
        }

        if (batch.Count > 0)
        {
            foreach (var artistId in await FilterNeedingFromVersionsAsync(batch, cancellationToken))
            {
                yield return artistId;
            }
        }
    }

    public async Task<int> ClearProjectedStreamVersionsAsync(CancellationToken cancellationToken = default)
    {
        const int pageSize = 1_024;
        const string artistPrefix = "catalog/artists/";
        var cleared = 0;
        string? lastId = null;
        ProjectedVersionCache.Clear();

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var session = documentStore.OpenAsyncSession();
            var page = (await session.Advanced.LoadStartingWithAsync<CatalogArtistRecordDto>(
                artistPrefix,
                start: 0,
                pageSize: pageSize,
                startAfter: lastId,
                token: cancellationToken)).ToArray();
            if (page.Length == 0)
            {
                break;
            }

            foreach (var artist in page)
            {
                lastId = artist.Id;
                if (artist.ProjectedStreamVersion is null)
                {
                    continue;
                }

                artist.ProjectedStreamVersion = null;
                cleared++;

                session.Delete(ArtistCatalogProjectionSnapshotDto.GetDocumentId(artist.ArtistId));
            }

            await session.SaveChangesAsync(cancellationToken);

            if (page.Length < pageSize)
            {
                break;
            }
        }

        logger.LogInformation(
            "Cleared ProjectedStreamVersion on {Cleared} catalog artist documents for projection repair.",
            cleared);
        return cleared;
    }

    private async Task<List<ArtistId>> FilterNeedingFromVersionsAsync(
        IReadOnlyList<(ArtistId ArtistId, int StreamVersion)> batch,
        CancellationToken cancellationToken)
    {
        var needed = new List<ArtistId>(batch.Count);
        var requiresRaven = new List<(ArtistId ArtistId, int StreamVersion)>(batch.Count);
        foreach (var (artistId, streamVersion) in batch)
        {
            if (ProjectedVersionCache.TryGetValue(artistId.Value, out var cached) &&
                cached >= streamVersion)
            {
                continue;
            }

            requiresRaven.Add((artistId, streamVersion));
        }

        if (requiresRaven.Count == 0)
        {
            return needed;
        }

        using var session = documentStore.OpenAsyncSession();
        var documentIds = requiresRaven
            .Select(pair => CatalogArtistRecordDto.GetDocumentId(pair.ArtistId.Value))
            .ToArray();
        var existing = await session.LoadAsync<CatalogArtistRecordDto>(documentIds, cancellationToken);
        foreach (var (artistId, streamVersion) in requiresRaven)
        {
            existing.TryGetValue(
                CatalogArtistRecordDto.GetDocumentId(artistId.Value),
                out var dto);
            var projectedVersion = dto?.ProjectedStreamVersion;
            if (projectedVersion is int version && version >= streamVersion)
            {
                ProjectedVersionCache[artistId.Value] = streamVersion;
                continue;
            }

            needed.Add(artistId);
        }

        return needed;
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
