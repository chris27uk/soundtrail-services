using Soundtrail.Domain.Catalog.MusicBrainzDumpImport;

namespace Soundtrail.Services.Enrichment.Scheduler.Features.ImportMusicBrainzDump.Ports;

public interface IMusicBrainzDumpSnapshotCatalog
{
    Task<MusicBrainzDumpSnapshotId> GetLatestSnapshotIdAsync(
        CancellationToken cancellationToken = default);

    Task<bool> SnapshotExistsAsync(
        MusicBrainzDumpSnapshotId snapshotId,
        CancellationToken cancellationToken = default);
}
