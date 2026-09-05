namespace Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard.Ports;

/// <summary>
/// One JSONL row plus the file byte position immediately after the line terminator.
/// </summary>
public readonly record struct MusicBrainzDumpShardLine(string Text, long EndByteOffset);
