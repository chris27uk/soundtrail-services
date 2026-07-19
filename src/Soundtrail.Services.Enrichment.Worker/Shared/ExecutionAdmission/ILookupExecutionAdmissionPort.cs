using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Common;

namespace Soundtrail.Services.Enrichment.Worker.Shared.ExecutionAdmission;

public interface ILookupExecutionAdmissionPort
{
    Task<LookupExecutionAdmissionResult> TryAcquireAsync(
        LookupExecutionAdmissionRequest request,
        CancellationToken cancellationToken);

    Task CommitAsync(
        CommandId commandId,
        CancellationToken cancellationToken);

    Task ReleaseAsync(
        CommandId commandId,
        CancellationToken cancellationToken);
}
