using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Events;
using Soundtrail.Domain.Search;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogCandidateIdentified.Support;

namespace Soundtrail.Services.Internal.Projector.Features.OnCatalogCandidateIdentified;

public sealed class ApplyCatalogCandidateIdentifiedToWorkHandler(
    IEventStreamRepository<MusicCatalogId, IDomainEvent> workRepository)
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
            loadedWork.Aggregate.CandidateIdentified(
                @event.TrustLevel,
                @event.RiskScore,
                @event.StartedAt);

            await loadedWork.Aggregate.SaveAsync(
                workRepository,
                loadedWork.Stream,
                operationId,
                cancellationToken);
        }
    }
}
