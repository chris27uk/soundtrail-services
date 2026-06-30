using Soundtrail.Contracts.Common;

namespace Soundtrail.Domain.Enrichment;

public readonly record struct MusicCatalogLookupId(MusicCatalogId Value) : IValueType
{
    public string StableValue => Value.Value;

    public static MusicCatalogLookupId From(MusicCatalogId value) => new(value);
}
