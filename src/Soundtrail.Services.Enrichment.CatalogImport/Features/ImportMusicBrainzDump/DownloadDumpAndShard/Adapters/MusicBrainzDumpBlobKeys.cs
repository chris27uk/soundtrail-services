using Soundtrail.Domain.Catalog.MusicBrainzDumpImport;

namespace Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard.Adapters;

public static class MusicBrainzDumpBlobKeys
{
    public static string Archive(string dumpVersion, string entityName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dumpVersion);
        ArgumentException.ThrowIfNullOrWhiteSpace(entityName);
        return $"{dumpVersion.Trim()}/{entityName.Trim()}.tar.xz";
    }

    public static string Shard(
        MusicBrainzDumpImportJobId jobId,
        MusicBrainzDumpImportPhase phase,
        int shardId)
    {
        var safeJob = jobId.Value.Replace(":", "_", StringComparison.Ordinal);
        return $"{safeJob}/{phase}/{shardId}.jsonl";
    }
}
