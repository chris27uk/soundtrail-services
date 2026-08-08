namespace Soundtrail.Domain.Abstractions;

public interface ISchedulerHandler<in TCommand>
{
    Task HandleAsync(TCommand command, CancellationToken cancellationToken = default);
}
