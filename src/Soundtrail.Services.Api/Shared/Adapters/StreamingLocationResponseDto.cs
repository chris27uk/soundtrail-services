namespace Soundtrail.Services.Api.Shared.Adapters;

public sealed record StreamingLocationResponseDto(
    string Provider,
    string? ExternalId,
    string Url);
