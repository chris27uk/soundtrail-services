using Soundtrail.Contracts;

namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Idempotency;

public sealed record ActiveLookupWork(CommandId CommandId, DateTimeOffset ExpiresAt);
