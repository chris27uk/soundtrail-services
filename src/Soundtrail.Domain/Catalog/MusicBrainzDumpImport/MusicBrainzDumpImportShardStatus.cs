namespace Soundtrail.Domain.Catalog.MusicBrainzDumpImport;

public enum MusicBrainzDumpImportShardStatus
{
    Pending = 0,
    Leased = 1,
    Completed = 2,
    Failed = 3
}
