using Soundtrail.Contracts.Common;

namespace Soundtrail.Domain.Events;

public sealed record ProviderReferenceLookupFailed(
    ProviderName Provider,
    ProviderName SourceProvider,
    DateTimeOffset ObservedAt) : IMusicTrackEvent;
