namespace Soundtrail.Domain.Abstractions;

public interface ICommandBus
{
    Task SendAsync(IMessage message, CancellationToken cancellationToken = default);
}
