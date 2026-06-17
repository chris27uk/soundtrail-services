using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Model;

namespace Soundtrail.Domain.Responses;

public sealed record ProviderReference(
    ProviderName Provider,
    Uri Url,
    string? ExternalId,
    ProviderName SourceProvider);
