using Soundtrail.Contracts.IntegrationMessaging.Commands;
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
            "search" => searchResultsHandler.Handle(
                context.WithMessage(typeRegistry.ToDomainObject<LookupMusicbrainzSearchResultsMessage>(context.Message)),
                cancellationToken),
            "artist-albums" => artistAlbumsHandler.Handle(
                context.WithMessage(typeRegistry.ToDomainObject<LookupMusicbrainzArtistAlbumsMessage>(context.Message)),
                cancellationToken),
            "artist-tracks" => artistTracksHandler.Handle(
                context.WithMessage(typeRegistry.ToDomainObject<LookupMusicbrainzArtistTracksMessage>(context.Message)),
                cancellationToken),
            "album-tracks" => albumTracksHandler.Handle(
                context.WithMessage(typeRegistry.ToDomainObject<LookupMusicbrainzAlbumTracksMessage>(context.Message)),
                cancellationToken),
            _ => throw new InvalidOperationException($"Unsupported MusicBrainz lookup kind '{context.Message.LookupKind}'.")
        };
    }
}
