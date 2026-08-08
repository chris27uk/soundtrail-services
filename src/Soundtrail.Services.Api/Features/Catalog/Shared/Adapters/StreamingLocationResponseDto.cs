namespace Soundtrail.Services.Api.Features.Catalog.Shared.Adapters;

public sealed record StreamingLocationResponseDto(
    string Provider,
    string? ExternalId,
    string Url);
