using Soundtrail.Domain.Common;

namespace Soundtrail.Domain.Catalog;

/// <summary>
/// Provenance identifier for a catalog item in the form <c>System:Id</c> (split on the first colon).
/// </summary>
public readonly record struct SourceSystemId : IValueType
{
    public const string MusicBrainzSystem = "musicbrainz";

    public SourceSystemId(string system, string id)
    {
        if (string.IsNullOrWhiteSpace(system))
        {
            throw new ArgumentException("Source system is required.", nameof(system));
        }

        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Source id is required.", nameof(id));
        }

        if (system.Contains(':', StringComparison.Ordinal))
        {
            throw new ArgumentException("Source system must not contain ':'.", nameof(system));
        }

        System = system.Trim();
        Id = id.Trim();
    }

    public string System { get; }

    public string Id { get; }

    public string StableValue => $"{System}:{Id}";

    public static SourceSystemId MusicBrainz(string mbid) => new(MusicBrainzSystem, mbid);

    public static SourceSystemId Parse(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Source system id is required.", nameof(value));
        }

        var trimmed = value.Trim();
        var separator = trimmed.IndexOf(':');
        if (separator <= 0 || separator == trimmed.Length - 1)
        {
            throw new ArgumentException(
                "Source system id must be in the form 'System:Id'.",
                nameof(value));
        }

        return new SourceSystemId(trimmed[..separator], trimmed[(separator + 1)..]);
    }

    public static bool TryParse(string? value, out SourceSystemId sourceSystemId)
    {
        sourceSystemId = default;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        try
        {
            sourceSystemId = Parse(value);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    public override string ToString() => StableValue;
}
