namespace Soundtrail.Domain.Abstractions;

public interface IHandler<in TRequest>
{
    Task Handle(TRequest request, CancellationToken cancellationToken = default);
}