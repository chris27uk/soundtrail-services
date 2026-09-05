namespace Soundtrail.Domain.Catalog.MusicBrainzDumpImport;

public sealed class MusicBrainzDumpOptions
{
    public const string SectionName = "MusicBrainzDump";

    public const string LocalStorage = "Local";

    public const string BlobStorage = "Blob";

    public const string DefaultBlobContainerName = "musicbrainz-dumps";

    public const string DefaultBlobConnectionName = "musicbrainz-dumps";

    /// <summary>
    /// <see cref="LocalStorage"/> (default) or <see cref="BlobStorage"/> (Azurite or Azure).
    /// </summary>
    public string Storage { get; set; } = LocalStorage;

    /// <summary>
    /// Azure Blob / Azurite connection string. When empty, falls back to ConnectionStrings:musicbrainz-dumps.
    /// </summary>
    public string? BlobConnectionString { get; set; }

    public string BlobContainerName { get; set; } = DefaultBlobContainerName;

    public string? LocalPath { get; set; }

    /// <summary>
    /// Path to release-group JSONL. When unset, inferred as sibling <c>release-group.jsonl</c> beside <see cref="LocalPath"/>.
    /// </summary>
    public string? ReleaseGroupsLocalPath { get; set; }

    /// <summary>
    /// Path to official release JSONL. When unset, inferred as sibling <c>release.jsonl</c> beside <see cref="LocalPath"/>.
    /// Used to materialize denormalized track JSONL when a prebuilt track source is absent.
    /// </summary>
    public string? ReleasesLocalPath { get; set; }

    /// <summary>
    /// Path to denormalized track-graph JSONL. When unset, inferred as sibling <c>track.jsonl</c> beside <see cref="LocalPath"/>.
    /// </summary>
    public string? TracksLocalPath { get; set; }

    /// <summary>
    /// Root directory for per-version archives (<c>{ArchiveDirectory}/{dumpVersion}/{entity}.tar.xz</c>) and extracted JSONL.
    /// </summary>
    public string? ArchiveDirectory { get; set; }

    /// <summary>
    /// HTTPS origin for official (or simulator) JSON dump archives when a versioned archive is not already cached.
    /// </summary>
    public string BaseUrl { get; set; } = "https://data.metabrainz.org/pub/musicbrainz/data/json-dumps";

    public string? ShardDirectory { get; set; }

    public int ShardCount { get; set; } = 4;

    /// <summary>
    /// Concurrent shard import workers within one CatalogImport process.
    /// Shards are artist-partitioned, so workers do not contend on the same streams.
    /// Defaults to <see cref="ShardCount"/> when unset or less than 1.
    /// </summary>
    public int ShardImportMaxDegreeOfParallelism { get; set; }

    /// <summary>
    /// Mapped rows to buffer before flushing ArtistCatalog appends and read-model BulkInsert.
    /// </summary>
    public int BulkInsertBatchSize { get; set; } = 2_000;

    /// <summary>
    /// Distinct artists to append per Raven <c>SaveChangesAsync</c> during dump import.
    /// Higher values cut round-trips; keep within session request budget (~2 loads/artist + 1 save).
    /// </summary>
    public int EventAppendArtistsPerSaveChanges { get; set; } = 64;

    /// <summary>
    /// Artists to load and buffer before one Raven BulkInsert during deferred projection.
    /// Each artist writes several catalog docs (browse + search), so 2_000 artists is ~8k documents.
    /// </summary>
    public int ProjectionArtistsPerBulkInsert { get; set; } = 2_000;

    /// <summary>
    /// Concurrent artist loads during deferred projection within one shard worker.
    /// When less than 1, uses <see cref="Environment.ProcessorCount"/>.
    /// </summary>
    public int ProjectionMaxDegreeOfParallelism { get; set; } = 16;

    /// <summary>
    /// When true, deferred projection also writes per-track browse docs and search candidates.
    /// Disable for large dump imports; artist/album aggregate browse docs remain.
    /// </summary>
    public bool WriteIndividualTrackAndSearchDocsOnProjection { get; set; } = false;

    /// <summary>
    /// When true, deferred projection enqueues low-priority streaming-location lookups for tracks
    /// that still have none. Disable for large dump imports to avoid flooding the lookup queue.
    /// </summary>
    public bool EnqueueStreamingLocationLookupsOnProjection { get; set; } = true;

    public TimeSpan LeaseDuration { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Parallel workers that parse/join release lines during the Recordings producer.
    /// Decode remains single-threaded. When less than 1, uses <see cref="Environment.ProcessorCount"/>.
    /// </summary>
    public int RecordingsJoinMaxDegreeOfParallelism { get; set; }

    /// <summary>
    /// When true, Recordings projection-only membership prefers Raven stream-metadata
    /// enumeration over Artists <c>.ids</c> / JSONL. Default false: Raven full-prefix
    /// scans thrash large databases and stall heartbeats when most artists are already projected.
    /// </summary>
    public bool PreferRavenArtistMembershipEnumeration { get; set; }

    /// <summary>
    /// When true, shard import streams JSONL from blob without downloading the shard file.
    /// When null, Azure connection strings stream; Azurite / development storage downloads to disk.
    /// </summary>
    public bool? StreamShardLinesFromBlob { get; set; }
}

