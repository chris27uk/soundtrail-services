using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Common;

namespace Soundtrail.Services.Enrichment.Worker.Shared.ExecutionAdmission;

public interface ILookupExecutionAdmissionPort
{
    Task<LookupExecutionAdmissionResult> TryAcquireAsync(
        LookupExecutionAdmissionRequest request,
        CancellationToken cancellationToken);

    Task CommitAsync(
        MessageId messageId,
        CancellationToken cancellationToken);

    Task ReleaseAsync(
        MessageId messageId,
        CancellationToken cancellationToken);
}
