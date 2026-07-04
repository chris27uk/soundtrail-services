using Soundtrail.Contracts.Common;

namespace Soundtrail.Domain.Catalog.Events;

public sealed record StreamingLocationDiscovered(
    MusicCatalogId? MusicCatalogId,
    ProviderName Provider,
    string? ExternalId,
    Uri Url,
    LookupSource SourceProvider,
    DateTimeOffset ObservedAt) : IMusicTrackEvent;
