namespace Soundtrail.Domain.Discovery;

public interface IReserveSourceApiBudgetPort
{
    Task<SourceApiBudgetReservationResult> TryReserveAsync(
        SourceApiBudgetReservationRequest request,
        CancellationToken cancellationToken);
}
