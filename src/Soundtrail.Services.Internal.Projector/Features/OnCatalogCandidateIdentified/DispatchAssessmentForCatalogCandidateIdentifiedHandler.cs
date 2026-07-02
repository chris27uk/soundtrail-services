using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Discovery.Commands;
using Soundtrail.Domain.Discovery.Events;
using Soundtrail.Domain.Search;
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

            await commandBus.SendAsync(
                new AssessMusicTrackCommand(
                    AssessMusicTrackCommand.Id(@event.MusicCatalogId, @event.StartedAt),
                    @event.CorrelationId,
                    @event.StartedAt,
                    LookupPriorityBand.Low,
                    @event.MusicCatalogId,
                    @event.SearchCriteria,
                    @event.TrustLevel,
                    @event.RiskScore),
                cancellationToken);
        }
    }
}
