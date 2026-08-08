using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Adapters.TypeRegistry;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Discovery.Messages;

namespace Soundtrail.Services.Enrichment.Worker.Infrastructure.Messaging;

public sealed class StreamingLocationLookupCommandHandler(
    ITypeRegistry typeRegistry,
    IHandler<LookupStreamingLocationByIsrcMessage> lookupByIsrcHandler,
    IHandler<LookupStreamingLocationByTrackMetadataMessage> lookupByTrackMetadataHandler) : IHandler<StreamingLocationLookupCommandDto>
{
    public Task Handle(IncomingMessage<StreamingLocationLookupCommandDto> context, CancellationToken cancellationToken = default)
    {
        return context.Message.LookupKind switch
        {
            "isrc" => lookupByIsrcHandler.Handle(
                context.WithMessage(typeRegistry.ToDomainObject<LookupStreamingLocationByIsrcMessage>(context.Message)),
                cancellationToken),
            "track-metadata" => lookupByTrackMetadataHandler.Handle(
                context.WithMessage(typeRegistry.ToDomainObject<LookupStreamingLocationByTrackMetadataMessage>(context.Message)),
                cancellationToken),
            _ => throw new InvalidOperationException($"Unsupported streaming location lookup kind '{context.Message.LookupKind}'.")
        };
    }
}
