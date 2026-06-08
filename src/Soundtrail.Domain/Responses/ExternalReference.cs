using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Model;

namespace Soundtrail.Domain.Responses;

public sealed record ExternalReference(
    ProviderName Provider,
    Uri Url,
    string? ExternalId,
    ReferenceConfidence Confidence);
