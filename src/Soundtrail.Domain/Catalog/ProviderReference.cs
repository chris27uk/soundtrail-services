using Soundtrail.Contracts.Common;

namespace Soundtrail.Domain.Catalog;

public sealed record ProviderReference(
    ProviderName Provider,
    string ProviderEntityType,
    string ProviderId,
    Uri Url,
    DateTimeOffset DiscoveredAt);
