namespace Soundtrail.Services.StreamBrowser;

internal static class StreamKinds
{
    public const string Work = "work";
    public const string Catalog = "catalog";

    public const string WorkAggregateType = "catalog-stream";
    public const string CatalogAggregateType = "artist-catalog-stream";

    public static string AggregateType(string kind) =>
        kind.Equals(Catalog, StringComparison.OrdinalIgnoreCase)
            ? CatalogAggregateType
            : WorkAggregateType;

    public static string KindFromAggregateType(string aggregateType) =>
        aggregateType.Equals(CatalogAggregateType, StringComparison.Ordinal)
            ? Catalog
            : Work;

    public static string MetadataPrefix(string kind) => $"{AggregateType(kind)}-streams/";

    public static string EventPrefix(string kind, string streamId) =>
        $"{AggregateType(kind)}-events/{streamId}/";
}
