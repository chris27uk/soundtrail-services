using Soundtrail.Contracts.Common;

namespace Soundtrail.Domain.Responses;

public sealed record ProviderLookupFailure(
    ProviderName Provider,
    ProviderName SourceProvider);
