using Soundtrail.Contracts.Common;

namespace Soundtrail.Domain.Enrichment.Responses;

public sealed record ExternalReference(ProviderName Provider, Uri Url, string? ExternalId);
