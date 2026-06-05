using Raven.Client.Documents.Session;
using Soundtrail.Services.Enrichment.Features.Execution.AppleLookupExecution;
using Soundtrail.Services.Enrichment.Shared.Orchestration;
using Wolverine.Attributes;

namespace Soundtrail.Services.Enrichment.Worker.Infrastructure.Messaging;

public sealed class AppleLookupExecutionListener(ExecuteAppleLookupHandler handler)
{
    [WolverineHandler]
    [Transactional]
    public async Task<object[]> Handle(
        HighPriorityVerifyApplePlaybackReferenceCommandMessage message,
        IAsyncDocumentSession _,
        CancellationToken cancellationToken = default)
    {
        var result = await handler.Handle(message.Command, cancellationToken);
        return result.Response is null ? [] : [result.Response];
    }

    [WolverineHandler]
    [Transactional]
    public async Task<object[]> Handle(
        LowPriorityVerifyApplePlaybackReferenceCommandMessage message,
        IAsyncDocumentSession _,
        CancellationToken cancellationToken = default)
    {
        var result = await handler.Handle(message.Command, cancellationToken);
        return result.Response is null ? [] : [result.Response];
    }
}
