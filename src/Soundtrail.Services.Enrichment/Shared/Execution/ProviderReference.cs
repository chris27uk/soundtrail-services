namespace Soundtrail.Services.Enrichment.Shared.Execution;

public sealed record ProviderReference(
    ProviderName Provider,
    Uri Url,
    string? ExternalId,
    ReferenceConfidence Confidence,
    ProviderName SourceProvider);
