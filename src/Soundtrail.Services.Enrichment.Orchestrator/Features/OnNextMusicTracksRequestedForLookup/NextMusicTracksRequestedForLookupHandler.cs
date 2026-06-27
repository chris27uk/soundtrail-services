using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Discovery.Commands;
using Soundtrail.Domain.Enrichment.Commands;
using Soundtrail.Services.Enrichment.Orchestrator.Shared.Persistence;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnNextMusicTracksRequestedForLookup;

public sealed class NextMusicTracksRequestedForLookupHandler(IPotentialCatalogLookupWorkStore potentialCatalogLookupWorkStore, ICommandBus commandBus) : IHandler<RunDiscoveryBacklogSchedulingCommand>
{
    public async Task Handle(RunDiscoveryBacklogSchedulingCommand command, CancellationToken cancellationToken = default)
    {
        var candidates = await potentialCatalogLookupWorkStore.GetPlanningCandidatesAsync(command.CreatedAt, command.BatchSize, cancellationToken);
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
