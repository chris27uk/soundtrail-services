using Soundtrail.Contracts;

namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Execution;

public sealed record ExternalReference(
    ProviderName Provider,
    Uri Url,
    string? ExternalId,
    ReferenceConfidence Confidence);
