namespace Soundtrail.Services.Enrichment.Scheduler;

public interface ISchedulerHandler
{
    Task Handle(CancellationToken cancellationToken = default);
}
