using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Adapters.Messaging;
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
            "isrc" => HandleDomain(
                context,
                typeRegistry.ToDomainObject<LookupStreamingLocationByIsrcMessage>(context.Message),
                lookupByIsrcHandler,
                cancellationToken),
            "track-metadata" => HandleDomain(
                context,
                typeRegistry.ToDomainObject<LookupStreamingLocationByTrackMetadataMessage>(context.Message),
                lookupByTrackMetadataHandler,
                cancellationToken),
            _ => throw new InvalidOperationException($"Unsupported streaming location lookup kind '{context.Message.LookupKind}'.")
        };
    }

    private static Task HandleDomain<TDomain>(
        IncomingMessage<StreamingLocationLookupCommandDto> context,
        TDomain domainMessage,
        IHandler<TDomain> handler,
        CancellationToken cancellationToken)
        where TDomain : class
    {
        MessageTelemetry.SetDomainEventName(typeof(TDomain));
        return handler.Handle(context.WithMessage(domainMessage), cancellationToken);
    }
}
