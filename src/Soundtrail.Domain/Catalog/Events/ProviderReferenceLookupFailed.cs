using Soundtrail.Contracts.Common;

namespace Soundtrail.Domain.Catalog.Events;

public sealed record ProviderReferenceLookupFailed(
    ProviderName Provider,
    LookupSource SourceProvider,
    DateTimeOffset ObservedAt) : IMusicTrackEvent;
