using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Services.Enrichment.Worker.Infrastructure.Idempotency.Storage;
using Soundtrail.Services.Enrichment.Worker.Shared.ExecutionAdmission;

namespace Soundtrail.Services.Enrichment.Worker.Infrastructure.ExecutionAdmission;

internal sealed class LegacyLookupExecutionAdmissionPort(
    ILookupExecutionReceiptStore receiptStore,
    IReserveSourceApiBudgetPort budgetPort) : ILookupExecutionAdmissionPort
{
    public async Task<LookupExecutionAdmissionResult> TryAcquireAsync(
        LookupExecutionAdmissionRequest request,
        CancellationToken cancellationToken)
    {
        var began = await receiptStore.TryBeginAsync(request.CommandId, cancellationToken);
        if (!began)
        {
            return LookupExecutionAdmissionResult.Duplicate();
        }

        var reservation = await budgetPort.TryReserveAsync(
            new SourceApiBudgetReservationRequest(request.Provider, request.RequestedAt),
            cancellationToken);

        if (!reservation.Accepted)
        {
            await receiptStore.ReleaseAsync(request.CommandId, cancellationToken);
            return LookupExecutionAdmissionResult.Deferred(
                reservation.RetryAt ?? request.RequestedAt.AddSeconds(1),
                reservation.Reason);
        }

        return LookupExecutionAdmissionResult.Acquired();
    }

    public Task CommitAsync(
        CommandId commandId,
        CancellationToken cancellationToken) =>
        receiptStore.MarkCompletedAsync(commandId, cancellationToken);

    public Task ReleaseAsync(
        CommandId commandId,
        CancellationToken cancellationToken) =>
        receiptStore.ReleaseAsync(commandId, cancellationToken);
}
