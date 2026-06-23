using Soundtrail.Contracts.Common;

namespace Soundtrail.Domain.Events;

public sealed record ProviderReferenceDiscovered(
    ProviderName Provider,
    string? ExternalId,
    Uri Url,
    ProviderName SourceProvider,
    DateTimeOffset ObservedAt) : IMusicTrackEvent;
