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

    /// <summary>
    /// Observation time for catalog freshness, derived from the snapshot directory name.
    /// Official dumps use <c>YYYYMMDD-HHMMSS</c> (UTC). Month-only ids (<c>yyyy-MM</c>) use day 1 at midnight UTC.
    /// </summary>
    public DateTimeOffset ToObservedAtUtc()
    {
        var value = Value;
        if (value.Length >= 15 &&
            value[8] == '-' &&
            TryParseDigits(value.AsSpan(0, 4), out var year) &&
            TryParseDigits(value.AsSpan(4, 2), out var month) &&
            TryParseDigits(value.AsSpan(6, 2), out var day) &&
            TryParseDigits(value.AsSpan(9, 2), out var hour) &&
            TryParseDigits(value.AsSpan(11, 2), out var minute) &&
            TryParseDigits(value.AsSpan(13, 2), out var second))
        {
            return CreateUtc(year, month, day, hour, minute, second, value);
        }

        if (value.Length >= 8 &&
            (value.Length == 8 || value[8] == '-') &&
            TryParseDigits(value.AsSpan(0, 4), out year) &&
            TryParseDigits(value.AsSpan(4, 2), out month) &&
            TryParseDigits(value.AsSpan(6, 2), out day))
        {
            return CreateUtc(year, month, day, 0, 0, 0, value);
        }

        if (value.Length == 7 &&
            value[4] == '-' &&
            TryParseDigits(value.AsSpan(0, 4), out year) &&
            TryParseDigits(value.AsSpan(5, 2), out month))
        {
            return CreateUtc(year, month, 1, 0, 0, 0, value);
        }

        throw new ArgumentException(
            $"MusicBrainz dump snapshot id '{value}' does not contain a parseable observation timestamp.",
            nameof(Value));
    }

    public static bool TryGetObservedAtUtc(string? dumpVersion, out DateTimeOffset observedAt)
    {
        observedAt = default;
        if (!TryParse(dumpVersion, out var snapshotId))
        {
            return false;
        }

        try
        {
            observedAt = snapshotId.ToObservedAtUtc();
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private static bool TryParseDigits(ReadOnlySpan<char> span, out int value)
    {
        value = 0;
        foreach (var ch in span)
        {
            if (ch is < '0' or > '9')
            {
                return false;
            }
        }

        return int.TryParse(span, out value);
    }

    private static DateTimeOffset CreateUtc(
        int year,
        int month,
        int day,
        int hour,
        int minute,
        int second,
        string value)
    {
        try
        {
            return new DateTimeOffset(year, month, day, hour, minute, second, TimeSpan.Zero);
        }
        catch (ArgumentOutOfRangeException exception)
        {
            throw new ArgumentException(
                $"MusicBrainz dump snapshot id '{value}' does not contain a valid observation timestamp.",
                nameof(Value),
                exception);
        }
    }

    public override string ToString() => Value;
}
