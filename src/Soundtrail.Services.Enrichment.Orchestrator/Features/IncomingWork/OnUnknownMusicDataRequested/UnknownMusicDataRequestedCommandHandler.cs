using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Adapters.TypeRegistry;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Discovery;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.IncomingWork.OnUnknownMusicDataRequested;

public sealed class UnknownMusicDataRequestedCommandHandler(
    ITypeRegistry typeRegistry,
    IHandler<RequestUnknownMusicDataMessage> innerHandler) : IHandler<UnknownMusicDataRequestedCommandDto>
{
    public Task Handle(IncomingMessage<UnknownMusicDataRequestedCommandDto> context, CancellationToken cancellationToken = default) =>
        innerHandler.Handle(
            context.WithMessage(typeRegistry.ToDomainObject<RequestUnknownMusicDataMessage>(context.Message)),
            cancellationToken);
}
