namespace Soundtrail.Services.Enrichment.Scheduler.Infrastructure.Hosting;

public sealed class SystemTimeProvider : ITimeProvider
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
