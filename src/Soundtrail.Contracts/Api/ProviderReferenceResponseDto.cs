namespace Soundtrail.Contracts.Api;

public sealed record ProviderReferenceResponseDto(
    string Provider,
    string ProviderEntityType,
    string ProviderId,
    Uri Url,
    DateTimeOffset DiscoveredAt);
