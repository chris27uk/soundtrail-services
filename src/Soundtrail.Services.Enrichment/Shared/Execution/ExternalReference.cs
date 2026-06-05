namespace Soundtrail.Services.Enrichment.Shared.Execution;

public sealed record ExternalReference(
    ProviderName Provider,
    Uri Url,
    string? ExternalId,
    ReferenceConfidence Confidence);
