using Soundtrail.Contracts;
using Soundtrail.Contracts.Orchestrator;

namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Execution;

public sealed record ProviderReference(
    ProviderName Provider,
    Uri Url,
    string? ExternalId,
    ReferenceConfidenceDto ConfidenceDto,
    ProviderName SourceProvider);
