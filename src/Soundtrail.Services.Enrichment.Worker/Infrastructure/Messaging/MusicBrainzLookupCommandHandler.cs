using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Adapters.Messaging;
using Soundtrail.Adapters.TypeRegistry;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Discovery.Messages;

namespace Soundtrail.Services.Enrichment.Worker.Infrastructure.Messaging;

public sealed class MusicBrainzLookupCommandHandler(
    ITypeRegistry typeRegistry,
    IHandler<LookupMusicbrainzSearchResultsMessage> searchResultsHandler,
    IHandler<LookupMusicbrainzArtistAlbumsMessage> artistAlbumsHandler,
    IHandler<LookupMusicbrainzArtistTracksMessage> artistTracksHandler,
    IHandler<LookupMusicbrainzAlbumTracksMessage> albumTracksHandler) : IHandler<MusicBrainzLookupCommandDto>
{
    public Task Handle(IncomingMessage<MusicBrainzLookupCommandDto> context, CancellationToken cancellationToken = default)
    {
        return context.Message.LookupKind switch
        {
            "search" => HandleDomain(
                context,
                typeRegistry.ToDomainObject<LookupMusicbrainzSearchResultsMessage>(context.Message),
                searchResultsHandler,
                cancellationToken),
            "artist-albums" => HandleDomain(
                context,
                typeRegistry.ToDomainObject<LookupMusicbrainzArtistAlbumsMessage>(context.Message),
                artistAlbumsHandler,
                cancellationToken),
            "artist-tracks" => HandleDomain(
                context,
                typeRegistry.ToDomainObject<LookupMusicbrainzArtistTracksMessage>(context.Message),
                artistTracksHandler,
                cancellationToken),
            "album-tracks" => HandleDomain(
                context,
                typeRegistry.ToDomainObject<LookupMusicbrainzAlbumTracksMessage>(context.Message),
                albumTracksHandler,
                cancellationToken),
            _ => throw new InvalidOperationException($"Unsupported MusicBrainz lookup kind '{context.Message.LookupKind}'.")
        };
    }

    private static Task HandleDomain<TDomain>(
        IncomingMessage<MusicBrainzLookupCommandDto> context,
        TDomain domainMessage,
        IHandler<TDomain> handler,
        CancellationToken cancellationToken)
        where TDomain : class
    {
        MessageTelemetry.SetDomainEventName(typeof(TDomain));
        return handler.Handle(context.WithMessage(domainMessage), cancellationToken);
    }
}
