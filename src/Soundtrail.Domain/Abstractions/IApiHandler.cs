namespace Soundtrail.Domain.Abstractions
{
    public interface IApiHandler<in TRequest, TResponse>
    {
        Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken = default);
    }
}
