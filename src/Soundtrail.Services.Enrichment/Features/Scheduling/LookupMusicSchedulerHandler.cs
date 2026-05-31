using Soundtrail.Services.Enrichment.Features.Scheduling.Contracts;
using Soundtrail.Services.Enrichment.Features.Scheduling.Models;

namespace Soundtrail.Services.Enrichment.Features.Scheduling;

public sealed class LookupMusicSchedulerHandler(IMusicCatalogResolutionPort musicCatalogResolutionPort, IRankedMusicCandidateStore rankedMusicCandidateStore)
{
    public async Task<LookupMusicCommand?> Handle(LookupMusicRequest request, CancellationToken cancellationToken = default)
    {
        var musicCatalogId = await musicCatalogResolutionPort.ResolveAsync(request, cancellationToken);
        if (musicCatalogId is null)
        {
            throw new ResolutionFailedException();
        }

        var existing = await rankedMusicCandidateStore.FindByMusicCatalogIdAsync(musicCatalogId, cancellationToken);
        var rankedMusicCandidate = existing is null ? RankedMusicCandidate.Create(request, musicCatalogId) : existing.Register(request);
        await rankedMusicCandidateStore.UpsertAsync(rankedMusicCandidate, cancellationToken);
        
        if (!rankedMusicCandidate.IsPending)
        {
            return null;
        }

        if (!rankedMusicCandidate.IsEligibleAt(request.OccurredAt))
        {
            return null;
        }

        if (rankedMusicCandidate.IsSuspicious)
        {
            return null;
        }

        return ToLookupCommand(request, rankedMusicCandidate);
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