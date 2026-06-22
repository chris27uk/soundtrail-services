namespace Soundtrail.Domain;

public interface IHandler<in TRequest>
{
    Task Handle(TRequest request, CancellationToken cancellationToken = default);
}

public interface IApiHandler<in TRequest, TResponse>
{
    Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken = default);
}
