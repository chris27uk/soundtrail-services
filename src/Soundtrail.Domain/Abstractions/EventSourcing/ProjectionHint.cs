namespace Soundtrail.Domain.Abstractions.EventSourcing;

/// <summary>
/// Hint for projection subscribers. Dump import uses <see cref="BulkImport"/> so live CDC can exclude it.
/// </summary>
public readonly record struct ProjectionHint
{
    public const string LiveValue = "live";
    public const string BulkImportValue = "bulk-import";

    private ProjectionHint(string value) => Value = value;

    public string Value { get; }

    public static ProjectionHint Live { get; } = new(LiveValue);

    public static ProjectionHint BulkImport { get; } = new(BulkImportValue);

    public static ProjectionHint FromStored(string? value) =>
        string.Equals(value, BulkImportValue, StringComparison.Ordinal)
            ? BulkImport
            : Live;

    public override string ToString() => Value;
}
