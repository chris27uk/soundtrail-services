using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Discovery.Commands;
using Soundtrail.Services.Enrichment.Orchestrator.Shared.Persistence;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnNextMusicTracksRequestedForLookup;

public sealed class NextMusicTracksRequestedForLookupHandler(
    IPotentialCatalogLookupWorkStore potentialCatalogLookupWorkStore,
    ICommandBus commandBus)
{
    public async Task RunOnceAsync(DateTimeOffset now, int take, CancellationToken cancellationToken = default)
    {
        var candidates = await potentialCatalogLookupWorkStore.GetPlanningCandidatesAsync(now, take, cancellationToken);

        foreach (var candidate in candidates)
        {
            await commandBus.SendAsync(
                new AssessMusicTrackCommand(
                    AssessMusicTrackCommand.Id(candidate.MusicCatalogId, now),
                    CorrelationId.New(),
                    now,
                    LookupPriorityBand.Low,
                    candidate.MusicCatalogId),
                cancellationToken);
        }
    }
}
