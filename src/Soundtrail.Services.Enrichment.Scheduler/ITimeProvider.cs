namespace Soundtrail.Services.Enrichment.Scheduler;

public interface ITimeProvider
{
    DateTimeOffset UtcNow { get; }
}
