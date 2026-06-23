namespace Soundtrail.Domain;

public interface IHandler<in TRequest>
{
    Task Handle(TRequest request, CancellationToken cancellationToken = default);
}