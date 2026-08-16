using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog.MusicBrainzDumpImport;

namespace Soundtrail.Domain.Operations;

public sealed record ImportMusicBrainzDumpCommand(
    DateTimeOffset TriggeredAt,
    bool Manual,
    MusicBrainzDumpSnapshotId? SnapshotId = null) : IScheduledMessage
{
    public static ImportMusicBrainzDumpCommand ForScheduled(DateTimeOffset triggeredAt) =>
        new(triggeredAt, Manual: false);

    public static ImportMusicBrainzDumpCommand ForManual(
        DateTimeOffset triggeredAt,
        MusicBrainzDumpSnapshotId snapshotId) =>
        new(triggeredAt, Manual: true, snapshotId);
}
