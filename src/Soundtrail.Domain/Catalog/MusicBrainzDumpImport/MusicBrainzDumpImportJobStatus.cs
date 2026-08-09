namespace Soundtrail.Domain.Catalog.MusicBrainzDumpImport;

public enum MusicBrainzDumpImportJobStatus
{
    Pending = 0,
    Downloading = 1,
    Extracting = 2,
    Importing = 3,
    Completed = 4,
    Failed = 5,
    Cancelled = 6
}
