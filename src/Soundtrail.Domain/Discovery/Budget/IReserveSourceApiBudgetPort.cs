namespace Soundtrail.Domain.Discovery.Budget;

public interface IReserveSourceApiBudgetPort
{
    Task<SourceApiBudgetReservationResult> TryReserveAsync(
        SourceApiBudgetReservationRequest request,
        CancellationToken cancellationToken);
}
