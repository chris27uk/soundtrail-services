namespace Soundtrail.Services.Enrichment.Scheduler.Infrastructure.Scheduling;

public sealed record RunDiscoveryBacklogScheduling(
    DateTimeOffset Now,
    int Take);
