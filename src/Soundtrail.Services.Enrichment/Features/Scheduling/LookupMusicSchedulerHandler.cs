using Soundtrail.Services.Enrichment.Features.Scheduling.Contracts;
using Soundtrail.Services.Enrichment.Features.Scheduling.Models;
using Soundtrail.Services.Features.Search.Models;

namespace Soundtrail.Services.Enrichment.Features.Scheduling;

public sealed class LookupMusicSchedulerHandler(
    IMusicCatalogResolutionPort musicCatalogResolutionPort,
    IRankedMusicCandidateStore rankedMusicCandidateStore,
    ILookupMusicRequestDeadLetterPort lookupMusicRequestDeadLetterPort,
    ILookupMusicCommandQueue lookupMusicCommandQueue)
{
    public async Task Handle(LookupMusicRequest request, CancellationToken cancellationToken = default)
    {
        var musicCatalogId = await musicCatalogResolutionPort.ResolveAsync(request, cancellationToken);
        if (musicCatalogId is null)
        {
            await lookupMusicRequestDeadLetterPort.DeadLetterAsync(request,"resolution_failed", cancellationToken);
            return;
        }

        var existing = await rankedMusicCandidateStore.FindByMusicCatalogIdAsync(musicCatalogId, cancellationToken);
        var rankedMusicCandidate = existing is null ? RankedMusicCandidate.Create(request, musicCatalogId) : existing.Register(request);
        await rankedMusicCandidateStore.UpsertAsync(rankedMusicCandidate, cancellationToken);
        if (!rankedMusicCandidate.IsPending)
        {
            return;
        }

        if (!rankedMusicCandidate.IsEligibleAt(request.OccurredAt))
        {
            return;
        }

        if (rankedMusicCandidate.IsSuspicious)
        {
            return;
        }

        await lookupMusicCommandQueue.EnqueueAsync(ToLookupCommand(request, rankedMusicCandidate), cancellationToken);
    }

    private static LookupMusicCommand ToLookupCommand(LookupMusicRequest request, RankedMusicCandidate rankedMusicCandidate)
    {
        return new LookupMusicCommand(
            CommandId: Guid.NewGuid().ToString("N"),
            MusicCatalogId: rankedMusicCandidate.MusicCatalogId,
            Query: rankedMusicCandidate.Query,
            CreatedAt: request.OccurredAt,
            CorrelationId: request.CorrelationId);
    }
}
