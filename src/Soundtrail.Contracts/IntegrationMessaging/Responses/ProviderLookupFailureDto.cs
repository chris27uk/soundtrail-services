namespace Soundtrail.Contracts.IntegrationMessaging.Responses;

public sealed record ProviderLookupFailureDto(
    string Provider,
    string SourceProvider);
