using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Adapters.Messaging;
using Soundtrail.Adapters.TypeRegistry;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Discovery.Messages;

namespace Soundtrail.Services.Enrichment.Worker.Infrastructure.Messaging;

public sealed class PlaylistTracksLookupCommandHandler(
    ITypeRegistry typeRegistry,
    IHandler<LookupPlaylistTracksByProviderMessage> innerHandler) : IHandler<PlaylistTracksLookupCommandDto>
{
    public Task Handle(IncomingMessage<PlaylistTracksLookupCommandDto> context, CancellationToken cancellationToken = default)
    {
        var domainMessage = typeRegistry.ToDomainObject<LookupPlaylistTracksByProviderMessage>(context.Message);
        MessageTelemetry.SetDomainEventName(typeof(LookupPlaylistTracksByProviderMessage));
        return innerHandler.Handle(context.WithMessage(domainMessage), cancellationToken);
    }
}
