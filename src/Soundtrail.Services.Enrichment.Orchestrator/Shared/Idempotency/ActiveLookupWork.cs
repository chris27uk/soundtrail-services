using Soundtrail.Domain.Common;

namespace Soundtrail.Services.Enrichment.Orchestrator.Shared.Idempotency;

public sealed record ActiveLookupWork(MessageId MessageId, DateTimeOffset ExpiresAt);
