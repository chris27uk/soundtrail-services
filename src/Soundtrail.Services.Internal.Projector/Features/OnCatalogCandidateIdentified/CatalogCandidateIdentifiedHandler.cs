using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Commands;
using Soundtrail.Domain.Discovery.Events;
using Soundtrail.Domain.Search;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogCandidateIdentified.Support;

namespace Soundtrail.Services.Internal.Projector.Features.OnCatalogCandidateIdentified;

public sealed class CatalogCandidateIdentifiedHandler(
    IEventStreamRepository<MusicCatalogId, IDomainEvent> workRepository,
    ICommandBus commandBus)
{
    public async Task Handle(
        CatalogCandidateIdentifiedCommand command,
        CancellationToken cancellationToken = default)
    {
        foreach (var item in command.Events.OrderBy(x => x.Version))
        {
            var @event = (CatalogCandidateIdentified)item.Event;
            var operationId = OperationId.From(
                $"CatalogCandidateIdentified:{DiscoveryQueryKey.StableValueFor(command.SearchCriteria)}:{item.Version}");

            var loadedWork = await CatalogDiscoveryWork.LoadAsync(
                workRepository,
                @event.MusicCatalogId,
                cancellationToken);
            loadedWork.Aggregate.RecordCandidateIdentified(
                @event.TrustLevel,
                @event.RiskScore,
                @event.StartedAt);

            var saved = await loadedWork.Aggregate.SaveAsync(
                workRepository,
                loadedWork.Stream,
                operationId,
                cancellationToken);
            if (!saved)
            {
                continue;
            }

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
