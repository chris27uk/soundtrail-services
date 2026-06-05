using Raven.Client.Documents.Session;
using Soundtrail.Services.Enrichment.Features.Execution;
using Soundtrail.Services.Enrichment.Shared.Execution;
using Wolverine.Attributes;

namespace Soundtrail.Services.Enrichment.Worker.Infrastructure.Messaging;

public sealed class YouTubeMusicLookupExecutionListener(ExecuteYouTubeMusicLookupHandler handler)
{
    [WolverineHandler]
    [Transactional]
    public Task Handle(
        HighPriorityYouTubeMusicLookupCommandMessage message,
        IAsyncDocumentSession _,
        CancellationToken cancellationToken = default) =>
        handler.Handle(message.Command, cancellationToken);

    [WolverineHandler]
    [Transactional]
    public Task Handle(
        LowPriorityYouTubeMusicLookupCommandMessage message,
        IAsyncDocumentSession _,
        CancellationToken cancellationToken = default) =>
        handler.Handle(message.Command, cancellationToken);
}
