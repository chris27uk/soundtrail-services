namespace Soundtrail.Services.Api.Features.Catalog.Shared.Contract;

public sealed record StreamingLocationResponse(
    string Provider,
    string? ExternalId,
    string Url);
