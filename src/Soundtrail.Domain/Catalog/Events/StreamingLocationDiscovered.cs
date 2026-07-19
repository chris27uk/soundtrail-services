using Soundtrail.Domain.Common;

namespace Soundtrail.Domain.Catalog.Events;

public sealed record StreamingLocationDiscovered(
    CatalogItemId? MusicCatalogId,
    CatalogTrackHierarchy Hierarchy,
    ProviderName Provider,
    string? ExternalId,
    Uri Url,
    LookupSource SourceProvider,
    DateTimeOffset ObservedAt) : IMusicTrackEvent;
