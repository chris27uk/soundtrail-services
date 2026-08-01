using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Adapters.TypeRegistry;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Discovery.Messages;

namespace Soundtrail.Services.Enrichment.Worker.Infrastructure.Messaging;

public sealed class PlaylistTracksLookupCommandHandler(
    ITypeRegistry typeRegistry,
    IHandler<LookupPlaylistTracksByProviderMessage> innerHandler) : IHandler<PlaylistTracksLookupCommandDto>
{
    public Task Handle(IncomingMessage<PlaylistTracksLookupCommandDto> context, CancellationToken cancellationToken = default) =>
        innerHandler.Handle(
            context.WithMessage(typeRegistry.ToDomainObject<LookupPlaylistTracksByProviderMessage>(context.Message)),
            cancellationToken);
}
