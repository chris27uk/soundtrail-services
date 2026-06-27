namespace Soundtrail.Domain.Abstractions;

public interface ICommandBus
{
    Task SendAsync(ICommand command, CancellationToken cancellationToken = default);
}
