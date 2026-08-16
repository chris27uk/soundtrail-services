using Soundtrail.Domain.Catalog.MusicBrainzDumpImport;
using Soundtrail.Services.Enrichment.Scheduler.Features.ImportMusicBrainzDump.Ports;

namespace Soundtrail.Services.Tests.Unit.Sociable.Infrastructure.Fakes;

internal sealed class MusicBrainzDumpSnapshotCatalogFake : IMusicBrainzDumpSnapshotCatalog
{
    private MusicBrainzDumpSnapshotId latest =
        MusicBrainzDumpSnapshotId.Parse("2026-08");

    private readonly HashSet<string> existing = new(StringComparer.Ordinal)
    {
        "2026-08"
    };

    public MusicBrainzDumpSnapshotCatalogFake WithLatest(string snapshotId)
    {
        latest = MusicBrainzDumpSnapshotId.Parse(snapshotId);
        existing.Add(latest.Value);
        return this;
    }

    public MusicBrainzDumpSnapshotCatalogFake WithExisting(params string[] snapshotIds)
    {
        foreach (var id in snapshotIds)
        {
            existing.Add(MusicBrainzDumpSnapshotId.Parse(id).Value);
        }

        return this;
    }

    public MusicBrainzDumpSnapshotCatalogFake WithoutExisting(string snapshotId)
    {
        existing.Remove(MusicBrainzDumpSnapshotId.Parse(snapshotId).Value);
        return this;
    }

    public Task<MusicBrainzDumpSnapshotId> GetLatestSnapshotIdAsync(
        CancellationToken cancellationToken = default) =>
        Task.FromResult(latest);

    public Task<bool> SnapshotExistsAsync(
        MusicBrainzDumpSnapshotId snapshotId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(existing.Contains(snapshotId.Value));
}
