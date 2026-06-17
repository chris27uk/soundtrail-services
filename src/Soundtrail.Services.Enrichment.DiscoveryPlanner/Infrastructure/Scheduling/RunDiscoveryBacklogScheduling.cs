namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Infrastructure.Scheduling;

public sealed record RunDiscoveryBacklogScheduling(DateTimeOffset Now, int Take);
