namespace Soundtrail.Adapters.Messaging;

public interface IMessageLifecycle
{
    Task CompleteAsync(CancellationToken cancellationToken);

    Task RetryAsync(TimeSpan delay, CancellationToken cancellationToken);

    Task DeadLetterAsync(string reason, string description, CancellationToken cancellationToken);
}
