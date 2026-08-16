using Soundtrail.Domain.Common;

namespace Soundtrail.Domain.Catalog.MusicBrainzDumpImport;

public readonly record struct MusicBrainzDumpImportJobId(string Value) : IValueType
{
    public string StableValue => Value;

    public static MusicBrainzDumpImportJobId ForSnapshot(MusicBrainzDumpSnapshotId snapshotId) =>
        new($"musicbrainz-dump:{snapshotId.Value}");

    public static MusicBrainzDumpImportJobId ForDumpVersion(string dumpVersion)
    {
        var snapshotId = MusicBrainzDumpSnapshotId.Parse(dumpVersion);
        return ForSnapshot(snapshotId);
    }

    public static MusicBrainzDumpImportJobId From(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Job id is required.", nameof(value));
        }

        return new(value.Trim());
    }

    public override string ToString() => Value;
}
