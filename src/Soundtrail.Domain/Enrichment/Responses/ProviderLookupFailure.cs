using Soundtrail.Contracts.Common;

namespace Soundtrail.Domain.Enrichment.Responses;

public sealed record ProviderLookupFailure(
    ProviderName Provider,
    LookupSource SourceProvider);
