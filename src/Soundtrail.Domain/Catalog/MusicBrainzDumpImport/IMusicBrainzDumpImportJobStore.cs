namespace Soundtrail.Domain.Catalog.MusicBrainzDumpImport;

public interface IMusicBrainzDumpImportJobStore
{
    Task<MusicBrainzDumpImportJob> EnsureAsync(
        MusicBrainzDumpImportJobId jobId,
        string dumpVersion,
        DateTimeOffset requestedAt,
        CancellationToken cancellationToken = default);

    Task<MusicBrainzDumpImportJob?> GetAsync(
        MusicBrainzDumpImportJobId jobId,
        CancellationToken cancellationToken = default);

    Task SaveAsync(
        MusicBrainzDumpImportJob job,
        CancellationToken cancellationToken = default);
}
