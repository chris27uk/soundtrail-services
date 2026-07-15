namespace Soundtrail.Domain.Discovery.Budget;

public interface ITryReserveSourceApiBudgetWindowPort
{
    Task<TryReserveSourceApiBudgetWindowResult> TryReserveAsync(
        TryReserveSourceApiBudgetWindowRequest request,
        CancellationToken cancellationToken);
}
