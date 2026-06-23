namespace Soundtrail.Domain;

public interface ICommandBus
{
    Task SendAsync(ICommand command, CancellationToken cancellationToken = default);
}
