using Soundtrail.Contracts;
using Soundtrail.Contracts.Orchestrator;

namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Execution;

public sealed record ExternalReference(
    ProviderName Provider,
    Uri Url,
    string? ExternalId,
    ReferenceConfidenceDto ConfidenceDto);
