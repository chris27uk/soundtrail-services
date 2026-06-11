using Soundtrail.Contracts.Common;

namespace Soundtrail.Domain.Events;

public sealed record ProviderPlaybackReferenceResolved(
    ProviderName Provider,
    string? ExternalId,
    Uri Url,
    ProviderName SourceProvider,
    DateTimeOffset ObservedAt) : IMusicTrackEvent;
