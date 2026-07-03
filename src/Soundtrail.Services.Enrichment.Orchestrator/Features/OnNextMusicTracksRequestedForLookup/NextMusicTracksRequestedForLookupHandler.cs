using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Commands;
using Soundtrail.Domain.Enrichment.Commands;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnNextMusicTracksRequestedForLookup;

public sealed class NextMusicTracksRequestedForLookupHandler(IDiscoveryBacklogPlanningReadPort discoveryBacklogPlanningReadPort, ICommandBus commandBus) : IHandler<RunDiscoveryBacklogSchedulingCommand>
{
    public async Task Handle(RunDiscoveryBacklogSchedulingCommand command, CancellationToken cancellationToken = default)
    {
        var candidates = await discoveryBacklogPlanningReadPort.GetPlanningCandidatesAsync(command.CreatedAt, command.BatchSize, cancellationToken);
        foreach (var candidate in candidates)
        {
            await commandBus.SendAsync(NewAssessmentCommand(command, candidate), cancellationToken);
        }
    }

    private static AssessMusicCatalogItemCommand NewAssessmentCommand(RunDiscoveryBacklogSchedulingCommand command, DiscoveryBacklogCandidate candidate)
    {
        var itemId = new CatalogItemId.Track(TrackId.From(candidate.MusicCatalogId.Value));
        var resource = CatalogItemResource.ForSearch(candidate.SearchCriteria);

        return new AssessMusicCatalogItemCommand(
            AssessMusicCatalogItemCommand.Id(itemId, resource, command.CreatedAt),
            CorrelationId.New(),
            command.CreatedAt,
            LookupPriorityBand.Low,
            itemId,
            resource);
    }
}
