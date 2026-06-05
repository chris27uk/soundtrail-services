using Raven.Client.Documents.Session;
using Soundtrail.Services.Enrichment.Features.Execution;
using Wolverine.Attributes;

namespace Soundtrail.Services.Enrichment.Worker.Infrastructure.Messaging;

public sealed class LookupExecutionListener(LookupExecutionHandler handler)
{
    [WolverineHandler]
    [Transactional]
    public Task Handle(
        ExecuteMusicBrainzLookupCommandMessage message,
        IAsyncDocumentSession _,
        CancellationToken cancellationToken = default) =>
        handler.Handle(message.Command, cancellationToken);

    [WolverineHandler]
    [Transactional]
    public Task Handle(
        ExecuteAppleLookupCommandMessage message,
        IAsyncDocumentSession _,
        CancellationToken cancellationToken = default) =>
        handler.Handle(message.Command, cancellationToken);

    [WolverineHandler]
    [Transactional]
    public Task Handle(
        ExecuteYouTubeMusicLookupCommandMessage message,
        IAsyncDocumentSession _,
        CancellationToken cancellationToken = default) =>
        handler.Handle(message.Command, cancellationToken);
}
