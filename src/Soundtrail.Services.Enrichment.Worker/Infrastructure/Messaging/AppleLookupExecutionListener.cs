using Raven.Client.Documents.Session;
using Soundtrail.Contracts.Worker;
using Soundtrail.Services.Enrichment.Worker.Features.Execution.AppleLookupExecution;
using Wolverine.Attributes;

namespace Soundtrail.Services.Enrichment.Worker.Infrastructure.Messaging;

public sealed class AppleLookupExecutionListener(ExecuteAppleLookupHandler handler)
{
    [WolverineHandler]
    [Transactional]
    public async Task<object[]> Handle(
        ResolveApplePlaybackReferenceCommandDto message,
        IAsyncDocumentSession _,
        CancellationToken cancellationToken = default)
    {
        var result = await handler.Handle(message, cancellationToken);
        return result.Response is null ? [] : [result.Response];
    }
}
