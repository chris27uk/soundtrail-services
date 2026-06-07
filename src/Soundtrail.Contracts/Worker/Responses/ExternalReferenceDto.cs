using Soundtrail.Contracts.Orchestrator;

namespace Soundtrail.Contracts.Worker.Responses;

public sealed record ExternalReferenceDto(
    string Provider,
    Uri Url,
    string? ExternalId,
    ReferenceConfidenceDto ConfidenceDto);
