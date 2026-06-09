namespace Soundtrail.Contracts.Responses;

public sealed record ExternalReferenceDto(
    string Provider,
    Uri Url,
    string? ExternalId,
    string Confidence);
