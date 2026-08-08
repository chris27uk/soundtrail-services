namespace Soundtrail.Services.Api.Shared.Contract;

public sealed record StreamingLocationResponse(
    string Provider,
    string? ExternalId,
    string Url);
