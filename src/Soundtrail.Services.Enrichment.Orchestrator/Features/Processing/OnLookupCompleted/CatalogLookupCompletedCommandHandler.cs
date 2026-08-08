using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Adapters.TypeRegistry;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Discovery.Messages;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.Processing.OnLookupCompleted;

public sealed class CatalogLookupCompletedCommandHandler(
    ITypeRegistry typeRegistry,
    LookupCompletedHandler innerHandler) : IHandler<CatalogLookupCompletedCommandDto>
{
    public Task Handle(IncomingMessage<CatalogLookupCompletedCommandDto> context, CancellationToken cancellationToken = default) =>
        innerHandler.Handle(
            context.WithMessage(typeRegistry.ToDomainObject<CatalogLookupCompleted>(context.Message)),
            cancellationToken);
}
