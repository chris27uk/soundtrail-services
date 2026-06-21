namespace Soundtrail.Domain.Discovery;

public interface ITryReserveSourceApiBudgetWindowPort
{
    Task<TryReserveSourceApiBudgetWindowResult> TryReserveAsync(
        TryReserveSourceApiBudgetWindowRequest request,
        CancellationToken cancellationToken);
}
