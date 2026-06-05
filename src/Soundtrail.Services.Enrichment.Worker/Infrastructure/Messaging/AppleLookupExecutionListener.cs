using Raven.Client.Documents.Session;
using Soundtrail.Services.Enrichment.Features.Execution;
using Soundtrail.Services.Enrichment.Shared.Execution;
using Wolverine.Attributes;

namespace Soundtrail.Services.Enrichment.Worker.Infrastructure.Messaging;

public sealed class AppleLookupExecutionListener(ExecuteAppleLookupHandler handler)
{
    [WolverineHandler]
    [Transactional]
    public Task Handle(
        HighPriorityAppleLookupCommandMessage message,
        IAsyncDocumentSession _,
        CancellationToken cancellationToken = default) =>
        handler.Handle(message.Command, cancellationToken);

    [WolverineHandler]
    [Transactional]
    public Task Handle(
        LowPriorityAppleLookupCommandMessage message,
        IAsyncDocumentSession _,
        CancellationToken cancellationToken = default) =>
        handler.Handle(message.Command, cancellationToken);
}
