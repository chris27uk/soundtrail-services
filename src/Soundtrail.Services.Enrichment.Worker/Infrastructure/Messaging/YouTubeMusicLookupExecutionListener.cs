using Raven.Client.Documents.Session;
using Soundtrail.Services.Enrichment.Features.Execution.YouTubeMusicLookupExecution;
using Soundtrail.Services.Enrichment.Shared.Orchestration;
using Wolverine.Attributes;

namespace Soundtrail.Services.Enrichment.Worker.Infrastructure.Messaging;

public sealed class YouTubeMusicLookupExecutionListener(ExecuteYouTubeMusicLookupHandler handler)
{
    [WolverineHandler]
    [Transactional]
    public async Task<object[]> Handle(
        HighPriorityResolveYouTubeMusicPlaybackReferenceCommandMessage message,
        IAsyncDocumentSession _,
        CancellationToken cancellationToken = default)
    {
        var result = await handler.Handle(message.Command, cancellationToken);
        return result.Response is null ? [] : [result.Response];
    }

    [WolverineHandler]
    [Transactional]
    public async Task<object[]> Handle(
        LowPriorityResolveYouTubeMusicPlaybackReferenceCommandMessage message,
        IAsyncDocumentSession _,
        CancellationToken cancellationToken = default)
    {
        var result = await handler.Handle(message.Command, cancellationToken);
        return result.Response is null ? [] : [result.Response];
    }
}
