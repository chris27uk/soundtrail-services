using Soundtrail.Contracts.Common;

namespace Soundtrail.Domain.Catalog.Events;

public sealed record ProviderReferenceLookupFailed(
    MusicCatalogId? MusicCatalogId,
    ProviderName Provider,
    LookupSource SourceProvider,
    DateTimeOffset ObservedAt) : IMusicTrackEvent
{
    public ProviderReferenceLookupFailed(
        ProviderName Provider,
        LookupSource SourceProvider,
        DateTimeOffset ObservedAt)
        : this(null, Provider, SourceProvider, ObservedAt)
    {
    }
}
