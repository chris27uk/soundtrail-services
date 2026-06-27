using Soundtrail.Contracts.Common;

namespace Soundtrail.Domain.Enrichment.Responses;

public sealed record ProviderReference(
    ProviderName Provider,
    Uri Url,
    string? ExternalId,
    ProviderName SourceProvider);
