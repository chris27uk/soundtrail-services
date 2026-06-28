using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Commands;
using Soundtrail.Domain.Enrichment.Commands;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnNextMusicTracksRequestedForLookup;

public sealed class NextMusicTracksRequestedForLookupHandler(ICatalogDiscoveryWorkPlanningReadPort discoveryWorkPlanningReadPort, ICommandBus commandBus) : IHandler<RunDiscoveryBacklogSchedulingCommand>
{
    public async Task Handle(RunDiscoveryBacklogSchedulingCommand command, CancellationToken cancellationToken = default)
    {
        var candidates = await discoveryWorkPlanningReadPort.GetPlanningCandidatesAsync(command.CreatedAt, command.BatchSize, cancellationToken);
        foreach (var candidate in candidates)
        {
            await commandBus.SendAsync(
                new AssessMusicTrackCommand(
                    AssessMusicTrackCommand.Id(candidate.MusicCatalogId, command.CreatedAt),
                    CorrelationId.New(),
                    command.CreatedAt,
                    LookupPriorityBand.Low,
                    candidate.MusicCatalogId),
                cancellationToken);
        }
    }
}
