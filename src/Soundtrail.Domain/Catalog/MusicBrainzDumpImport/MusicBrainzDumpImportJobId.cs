using Soundtrail.Domain.Common;

namespace Soundtrail.Domain.Catalog.MusicBrainzDumpImport;

public readonly record struct MusicBrainzDumpImportJobId(string Value) : IValueType
{
    public string StableValue => Value;

    public static MusicBrainzDumpImportJobId ForDumpVersion(string dumpVersion)
    {
        if (string.IsNullOrWhiteSpace(dumpVersion))
        {
            throw new ArgumentException("Dump version is required.", nameof(dumpVersion));
        }

        return new($"musicbrainz-dump:{dumpVersion.Trim()}");
    }

    public static MusicBrainzDumpImportJobId ForMonth(DateTimeOffset triggeredAt)
    {
        var utc = triggeredAt.ToUniversalTime();
        return ForDumpVersion($"{utc.Year:D4}-{utc.Month:D2}");
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
