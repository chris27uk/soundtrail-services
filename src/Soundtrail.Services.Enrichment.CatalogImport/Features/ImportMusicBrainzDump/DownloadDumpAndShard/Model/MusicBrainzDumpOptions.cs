namespace Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard.Model;

public sealed class MusicBrainzDumpOptions
{
    public const string SectionName = "MusicBrainzDump";

    public string Source { get; set; } = "http";

    public string? LocalPath { get; set; }
}
