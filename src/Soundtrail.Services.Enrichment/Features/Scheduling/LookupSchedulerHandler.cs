using Soundtrail.Services.Enrichment.Features.Scheduling.Contracts;
using Soundtrail.Services.Enrichment.Features.Scheduling.Extensions;
using Soundtrail.Services.Enrichment.Features.Scheduling.Models;

namespace Soundtrail.Services.Enrichment.Features.Scheduling;

public sealed class LookupSchedulerHandler(
    IMusicCatalogCandidateSearch musicCatalogCandidateSearch,
    IRankedMusicCandidateStore rankedMusicCandidateStore,
    LookupPlanner lookupPlanner,
    MusicCatalogResolutionPolicy musicCatalogResolutionPolicy)
{
    public async Task<LookupMusicCommand?> Handle(LookupMusicRequest request, CancellationToken cancellationToken = default)
    {
        var matches = await musicCatalogCandidateSearch.SearchAsync(request.Query, cancellationToken);
        var resolution = musicCatalogResolutionPolicy.Resolve(matches);
        if (!resolution.IsResolved)
        {
            throw new ResolutionFailedException(resolution.Outcome);
        }

        var musicCatalogId = resolution.MusicCatalogId!;
        var existing = await rankedMusicCandidateStore.FindByMusicCatalogIdAsync(musicCatalogId, cancellationToken);
        var rankedMusicCandidate = existing is null ? RankedMusicCandidate.Create(request, musicCatalogId) : existing.AcceptNewRequest(request);
        await rankedMusicCandidateStore.UpsertAsync(rankedMusicCandidate, cancellationToken);

        var plan = lookupPlanner.Plan(rankedMusicCandidate, request.OccurredAt);
        if (!plan.ShouldSchedule)
        {
            return null;
        }

        return request.ToCommand(musicCatalogId, plan.Priority!.Value);
    }
}
