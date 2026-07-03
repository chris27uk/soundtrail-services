using Soundtrail.Contracts.Common;

namespace Soundtrail.Domain.Catalog
{
    public sealed class StreamingLocation(
        ProviderName provider,
        string? externalId,
        Uri url,
        LookupSource sourceProvider,
        DateTimeOffset observedAt)
    {
        public ProviderName Provider { get; } = provider;

        public string? ExternalId { get; } = externalId;

        public Uri Url { get; } = url;

        public LookupSource SourceProvider { get; } = sourceProvider;

        public DateTimeOffset ObservedAt { get; } = observedAt;
    }
}
