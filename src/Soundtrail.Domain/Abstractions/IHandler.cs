namespace Soundtrail.Domain.Abstractions;

public interface IHandler<TRequest>
{
    Task Handle(IncomingMessage<TRequest> context, CancellationToken cancellationToken = default);
}
