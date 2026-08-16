namespace Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard.Model;

public sealed class MusicBrainzDumpOptions
{
    public const string SectionName = "MusicBrainzDump";

    public string Source { get; set; } = "local";

    public string? LocalPath { get; set; }

    /// <summary>
    /// Path to release-group JSONL. When unset, inferred as sibling <c>release-group.jsonl</c> beside <see cref="LocalPath"/>.
    /// </summary>
    public string? ReleaseGroupsLocalPath { get; set; }

    public string? ShardDirectory { get; set; }

    public int ShardCount { get; set; } = 4;

    public TimeSpan LeaseDuration { get; set; } = TimeSpan.FromMinutes(5);

    public DateTimeOffset? DumpObservedAt { get; set; }
}
