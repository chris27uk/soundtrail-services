using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Discovery.Events;
using Soundtrail.Domain.Enrichment.Commands;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnKnownAlbumRequested.Ports;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnKnownAlbumRequested.Support;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnKnownAlbumRequested;

public sealed class DispatchKnownAlbumLookupCommandHandler(
    ILoadKnownCatalogAlbumPort loadKnownCatalogAlbumPort,
    ICommandBus commandBus) : IHandler<AlbumCatalogLookupRequestedCommand>
{
    public async Task Handle(
        AlbumCatalogLookupRequestedCommand command,
        CancellationToken cancellationToken = default)
    {
        foreach (var item in command.Events)
        {
            if (item.Event is not CatalogLookupRequested requested)
            {
                continue;
            }

            var artistId = requested.ArtistId
                           ?? throw new InvalidOperationException(
                               $"Album lookup request '{command.DiscoveryStreamId}:{item.Version}' must include an artist id.");
            var knownAlbum = await loadKnownCatalogAlbumPort.LoadAsync(artistId, requested.AlbumId, cancellationToken)
                             ?? throw new InvalidOperationException(
                                 $"Known album '{requested.AlbumId.Value}' for artist '{artistId.Value}' must exist in the catalog before lookup can be dispatched.");

            await commandBus.SendAsync(
                new LookupAlbumMetadataCommand(
                    CommandId.For($"LookupAlbumMetadata:{artistId.Value}:{requested.AlbumId.Value}"),
                    artistId,
                    requested.AlbumId,
                    LookupPriorityBand.High,
                    requested.RequestedAt,
                    requested.CorrelationId,
                    knownAlbum.ArtistName,
                    knownAlbum.AlbumTitle,
                    knownAlbum.MusicBrainzReleaseId,
                    knownAlbum.MusicBrainzArtistId),
                cancellationToken);
        }
    }
}
