namespace Soundtrail.Domain.Abstractions;

public interface IScheduledMessageHandler<in TMessage>
    where TMessage : IScheduledMessage
{
    Task HandleAsync(TMessage message, CancellationToken cancellationToken = default);
}
