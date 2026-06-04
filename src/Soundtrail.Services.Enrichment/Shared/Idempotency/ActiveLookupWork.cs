using Soundtrail.Services.Shared;

namespace Soundtrail.Services.Enrichment.Features.JustInTimeScheduling.Idempotency;

public sealed record ActiveLookupWork(
    CommandId CommandId,
    DateTimeOffset ExpiresAt);
