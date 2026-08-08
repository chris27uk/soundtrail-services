using Soundtrail.Domain.Common;
using Soundtrail.Services.Enrichment.Worker.Shared.ExecutionAdmission;

namespace Soundtrail.Services.Tests.Unit.Sociable.Infrastructure.Fakes;

internal sealed class LookupExecutionAdmissionPortFake : ILookupExecutionAdmissionPort
{
    public LookupExecutionAdmissionResult Result { get; set; } = LookupExecutionAdmissionResult.Acquired();

    public LookupExecutionAdmissionRequest? RequestedAdmission { get; private set; }

    public List<MessageId> CommittedCommandIds { get; } = [];

    public List<MessageId> ReleasedCommandIds { get; } = [];

    public Task<LookupExecutionAdmissionResult> TryAcquireAsync(
        LookupExecutionAdmissionRequest request,
        CancellationToken cancellationToken)
    {
        RequestedAdmission = request;
        return Task.FromResult(Result);
    }

    public Task CommitAsync(MessageId messageId, CancellationToken cancellationToken)
    {
        CommittedCommandIds.Add(messageId);
        return Task.CompletedTask;
    }

    public Task ReleaseAsync(MessageId messageId, CancellationToken cancellationToken)
    {
        ReleasedCommandIds.Add(messageId);
        return Task.CompletedTask;
    }
}
