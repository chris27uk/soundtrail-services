namespace Soundtrail.Services.Enrichment.Worker.Infrastructure.Scheduling;

public sealed record RunDiscoveryBacklogScheduling(
    DateTimeOffset Now,
    int Take);
