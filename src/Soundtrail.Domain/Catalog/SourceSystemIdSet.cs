namespace Soundtrail.Domain.Catalog;

public static class SourceSystemIdSet
{
    public static HashSet<SourceSystemId> Create(params SourceSystemId[] ids) =>
        new(ids);

    public static HashSet<SourceSystemId> FromStableValues(IEnumerable<string>? values)
    {
        var set = new HashSet<SourceSystemId>();
        if (values is null)
        {
            return set;
        }

        foreach (var value in values)
        {
            if (SourceSystemId.TryParse(value, out var id))
            {
                set.Add(id);
            }
        }

        return set;
    }

    public static HashSet<SourceSystemId> FromLegacyMusicBrainz(string? mbid)
    {
        var set = new HashSet<SourceSystemId>();
        if (!string.IsNullOrWhiteSpace(mbid))
        {
            set.Add(SourceSystemId.MusicBrainz(mbid));
        }

        return set;
    }

    public static IReadOnlyList<string> ToStableValues(IEnumerable<SourceSystemId> ids) =>
        ids.Select(static id => id.StableValue).OrderBy(static x => x, StringComparer.Ordinal).ToArray();

    public static void UnionWith(HashSet<SourceSystemId> target, IEnumerable<SourceSystemId>? source)
    {
        if (source is null)
        {
            return;
        }

        foreach (var id in source)
        {
            // One id per system: replace any existing entry for the same system.
            target.RemoveWhere(existing =>
                string.Equals(existing.System, id.System, StringComparison.Ordinal));
            target.Add(id);
        }
    }

    public static string? MusicBrainzIdOrNull(IEnumerable<SourceSystemId> ids) =>
        ids.FirstOrDefault(static id =>
                string.Equals(id.System, SourceSystemId.MusicBrainzSystem, StringComparison.Ordinal))
            is { } musicBrainz
            ? musicBrainz.Id
            : null;
}
