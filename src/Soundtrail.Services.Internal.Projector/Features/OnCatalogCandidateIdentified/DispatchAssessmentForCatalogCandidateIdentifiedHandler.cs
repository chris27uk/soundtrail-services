using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Commands;
using Soundtrail.Domain.Discovery.Events;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogCandidateIdentified.Support;

namespace Soundtrail.Services.Internal.Projector.Features.OnCatalogCandidateIdentified;

public sealed class DispatchAssessmentForCatalogCandidateIdentifiedHandler(
    ICommandBus commandBus)
{
    public async Task Handle(
        CatalogCandidateIdentifiedCommand command,
        CancellationToken cancellationToken = default)
    {
        foreach (var item in command.Events.OrderBy(x => x.Version))
        {
            var @event = (CatalogCandidateIdentified)item.Event;
            var itemId = new CatalogItemId.Track(TrackId.From(@event.MusicCatalogId.Value));

            await commandBus.SendAsync(
                new AssessMusicCatalogItemCommand(
                    AssessMusicCatalogItemCommand.Id(
                        itemId,
                        CatalogItemResource.ForSearch(@event.SearchCriteria),
                        @event.StartedAt),
                    @event.CorrelationId,
                    @event.StartedAt,
                    LookupPriorityBand.Low,
                    itemId,
                    CatalogItemResource.ForSearch(@event.SearchCriteria),
                    @event.TrustLevel,
                    @event.RiskScore),
                cancellationToken);
        }
    }
}
