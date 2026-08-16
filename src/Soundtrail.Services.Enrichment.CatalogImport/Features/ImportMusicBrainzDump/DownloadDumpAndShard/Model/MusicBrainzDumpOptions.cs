namespace Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard.Model;

public sealed class MusicBrainzDumpOptions
{
    public const string SectionName = "MusicBrainzDump";

    public const string LocalStorage = "Local";

    public const string BlobStorage = "Blob";

    public const string DefaultBlobContainerName = "musicbrainz-dumps";

    public const string DefaultBlobConnectionName = "musicbrainz-dumps";

    public string Source { get; set; } = "local";

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
    /// Path to denormalized track-graph JSONL. When unset, inferred as sibling <c>track.jsonl</c> beside <see cref="LocalPath"/>.
    /// </summary>
    public string? TracksLocalPath { get; set; }

    /// <summary>
    /// Root directory for per-version archives (<c>{ArchiveDirectory}/{dumpVersion}/{entity}.tar.xz</c>) and extracted JSONL.
    /// </summary>
    public string? ArchiveDirectory { get; set; }

    /// <summary>
    /// Metabrainz JSON dump root URL used when <see cref="Source"/> is <c>http</c>.
    /// </summary>
    public string BaseUrl { get; set; } = "https://data.metabrainz.org/pub/musicbrainz/data/json-dumps";

    public string? ShardDirectory { get; set; }

    public int ShardCount { get; set; } = 4;

    public TimeSpan LeaseDuration { get; set; } = TimeSpan.FromMinutes(5);

    public DateTimeOffset? DumpObservedAt { get; set; }
}
