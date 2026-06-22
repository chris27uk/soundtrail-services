namespace Soundtrail.Services.Enrichment.Orchestrator.Infrastructure.Scheduling;

public sealed record RunDiscoveryBacklogScheduling(DateTimeOffset Now, int Take);
