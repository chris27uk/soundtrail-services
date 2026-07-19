using Soundtrail.Domain.Common;

namespace Soundtrail.Services.Enrichment.Orchestrator.Shared.Idempotency;

public sealed record ActiveLookupWork(CommandId CommandId, DateTimeOffset ExpiresAt);
