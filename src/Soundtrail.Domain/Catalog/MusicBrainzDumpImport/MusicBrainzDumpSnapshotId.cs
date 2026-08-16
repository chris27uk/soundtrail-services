using Soundtrail.Domain.Common;

namespace Soundtrail.Domain.Catalog.MusicBrainzDumpImport;

/// <summary>
/// Immutable MusicBrainz JSON dump snapshot directory id (e.g. <c>20260808-001002</c>).
/// Never a mutable pointer token such as <c>LATEST</c>.
/// </summary>
public readonly record struct MusicBrainzDumpSnapshotId : IValueType
{
    private MusicBrainzDumpSnapshotId(string value) => Value = value;

    public string Value { get; }

    public string StableValue => Value;

    public static MusicBrainzDumpSnapshotId Parse(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("MusicBrainz dump snapshot id is required.", nameof(value));
        }

        var trimmed = value.Trim();
        if (trimmed.Contains('/') ||
            trimmed.Contains('\\') ||
            trimmed.Contains("..", StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "MusicBrainz dump snapshot id must be a single directory name.",
                nameof(value));
        }

        if (IsPointerToken(trimmed))
        {
            throw new ArgumentException(
                "MusicBrainz dump snapshot id must be a concrete snapshot directory, not a LATEST pointer.",
                nameof(value));
        }

        return new MusicBrainzDumpSnapshotId(trimmed);
    }

    public static bool TryParse(string? value, out MusicBrainzDumpSnapshotId snapshotId)
    {
        snapshotId = default;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        try
        {
            snapshotId = Parse(value);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    public static bool IsPointerToken(string value) =>
        string.Equals(value, "LATEST", StringComparison.OrdinalIgnoreCase) ||
        value.StartsWith("latest-is-", StringComparison.OrdinalIgnoreCase);

    public override string ToString() => Value;
}
