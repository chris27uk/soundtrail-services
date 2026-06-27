using Soundtrail.Contracts.Common;

namespace Soundtrail.Domain.Catalog.Projection;

public sealed record ProjectedProviderReference(
    ProviderName Provider,
    Uri Url,
    string? ExternalId,
    LookupSource SourceProvider);
