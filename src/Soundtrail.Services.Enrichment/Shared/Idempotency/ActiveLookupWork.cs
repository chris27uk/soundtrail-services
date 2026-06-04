using Soundtrail.Services.Shared;

namespace Soundtrail.Services.Enrichment.Shared.Idempotency;

public sealed record ActiveLookupWork(
    CommandId CommandId,
    DateTimeOffset ExpiresAt);
