using Soundtrail.Contracts;
using Soundtrail.Contracts.Api;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.JustInTimeScheduling.Extensions;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.JustInTimeScheduling.Model;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Idempotency;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Persistence;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Prioritisation;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Search;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Search.Resolution;

namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.JustInTimeScheduling;

public sealed class LookupMusicRequestHandler(
    IMusicCatalogCandidateSearch musicCatalogCandidateSearch,
    IRankedMusicCandidateStore rankedMusicCandidateStore,
    DiscoveryPriorityPolicy discoveryPriorityPolicy,
    MusicCatalogResolutionPolicy musicCatalogResolutionPolicy,
    IActiveLookupWorkStore activeLookupWorkStore)
{
    private static readonly TimeSpan ActiveReservationDuration = TimeSpan.FromMinutes(15);

    public Task<LookupSchedulingResult> Handle(
        LookupMusicRequest request,
        CancellationToken cancellationToken = default) =>
        ScheduleAsync(request, cancellationToken);

    public async Task<LookupSchedulingResult> ScheduleAsync(
        LookupMusicRequest request,
        CancellationToken cancellationToken = default)
    {
        var matches = await musicCatalogCandidateSearch.SearchAsync(request.Query, cancellationToken);
        var resolution = musicCatalogResolutionPolicy.Resolve(matches);
        if (!resolution.IsResolved)
        {
            throw new ResolutionFailedException(resolution.Outcome);
        }

        var musicCatalogId = resolution.MusicCatalogId!;
        var existing = await rankedMusicCandidateStore.FindByMusicCatalogIdAsync(musicCatalogId, cancellationToken);
        var rankedMusicCandidate = existing is null
            ? RankedMusicCandidate.Create(request, musicCatalogId)
            : existing.AcceptNewRequest(request);
        await rankedMusicCandidateStore.UpsertAsync(rankedMusicCandidate, cancellationToken);

        var plan = discoveryPriorityPolicy.Investigate(rankedMusicCandidate, request.OccurredAt);
        if (!plan.ShouldSchedule)
        {
            return LookupSchedulingResult.DoNotSchedule();
        }

        var command = request.ToCommand(musicCatalogId, plan.Priority!.Value);
        var acquired = await activeLookupWorkStore.TryAcquireAsync(
            command.CommandId,
            request.OccurredAt.Add(ActiveReservationDuration),
            cancellationToken);

        return acquired
            ? LookupSchedulingResult.Schedule(command)
            : LookupSchedulingResult.DoNotSchedule();
    }
}
