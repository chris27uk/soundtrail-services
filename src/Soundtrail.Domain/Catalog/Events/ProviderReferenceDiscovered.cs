using Soundtrail.Contracts.Common;

namespace Soundtrail.Domain.Catalog.Events;

public sealed record ProviderReferenceDiscovered(
    MusicCatalogId? MusicCatalogId,
    ProviderName Provider,
    string? ExternalId,
    Uri Url,
    LookupSource SourceProvider,
    DateTimeOffset ObservedAt) : IMusicTrackEvent
{
    public ProviderReferenceDiscovered(
        ProviderName Provider,
        string? ExternalId,
        Uri Url,
        LookupSource SourceProvider,
        DateTimeOffset ObservedAt)
        : this(null, Provider, ExternalId, Url, SourceProvider, ObservedAt)
    {
    }
}
