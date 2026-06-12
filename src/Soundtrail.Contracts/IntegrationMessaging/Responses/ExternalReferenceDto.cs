namespace Soundtrail.Contracts.IntegrationMessaging.Responses;

public sealed record ExternalReferenceDto(
    string Provider,
    Uri Url,
    string? ExternalId);
