using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Discovery.Events;
using Soundtrail.Domain.Enrichment.Commands;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnKnownArtistRequested.Ports;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnKnownArtistRequested.Support;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnKnownArtistRequested;

public sealed class DispatchKnownArtistLookupCommandHandler(
    ILoadKnownCatalogArtistPort loadKnownCatalogArtistPort,
    ICommandBus commandBus) : IHandler<ArtistCatalogLookupRequestedCommand>
{
    public async Task Handle(
        ArtistCatalogLookupRequestedCommand command,
        CancellationToken cancellationToken = default)
    {
        foreach (var item in command.Events)
        {
            if (item.Event is not ArtistCatalogLookupRequested requested)
            {
                continue;
            }

            var knownArtist = await loadKnownCatalogArtistPort.LoadAsync(requested.ArtistId, cancellationToken)
                              ?? throw new InvalidOperationException(
                                  $"Known artist '{requested.ArtistId.Value}' must exist in the catalog before lookup can be dispatched.");

            await commandBus.SendAsync(
                new LookupArtistMetadataCommand(
                    CommandId.For($"LookupArtistMetadata:{requested.ArtistId.Value}"),
                    requested.ArtistId,
                    LookupPriorityBand.High,
                    requested.RequestedAt,
                    requested.CorrelationId,
                    knownArtist.ArtistName,
                    knownArtist.MusicBrainzArtistId),
                cancellationToken);
        }
    }
}
