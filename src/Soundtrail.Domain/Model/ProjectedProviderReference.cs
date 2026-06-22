using Soundtrail.Contracts.Common;

namespace Soundtrail.Domain.Model;

public sealed record ProjectedProviderReference(
    ProviderName Provider,
    Uri Url,
    string? ExternalId,
    ProviderName SourceProvider);
