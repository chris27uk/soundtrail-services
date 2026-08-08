using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Adapters.TypeRegistry;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Discovery.Messages;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.Processing.OnLookupWorkReady;

public sealed class DispatchLookupWorkCommandHandler(
    ITypeRegistry typeRegistry,
    LookupWorkReadyHandler innerHandler) : IHandler<DispatchLookupWorkCommandDto>
{
    public Task Handle(IncomingMessage<DispatchLookupWorkCommandDto> context, CancellationToken cancellationToken = default) =>
        innerHandler.Handle(
            context.WithMessage(typeRegistry.ToDomainObject<DispatchLookupWork>(context.Message)),
            cancellationToken);
}
