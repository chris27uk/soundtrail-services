using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.JustInTimeScheduling.LocalSearch;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.JustInTimeScheduling.Model;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Idempotency;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Persistence;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Prioritisation;
using Soundtrail.Contracts.Common;
using Soundtrail.Domain;
using Soundtrail.Domain.Commands;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Search;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Search.Resolution;
using ResolvePlaybackReferencesCommand = Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.JustInTimeScheduling.Model.ResolvePlaybackReferencesCommand;

namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.JustInTimeScheduling;

public sealed class LookupMusicRequestHandler(
    IMusicCatalogCandidateSearch musicCatalogCandidateSearch,
    IRankedMusicCandidateStore rankedMusicCandidateStore,
    DiscoveryPriorityPolicy discoveryPriorityPolicy,
    MusicCatalogMatchResolver musicCatalogMatchResolver,
    IActiveLookupWorkStore activeLookupWorkStore,
    ILocalMusicTrackSearch localMusicTrackSearch)
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
        var resolution = musicCatalogMatchResolver.Resolve(matches);
        if (!resolution.IsResolved)
        {
            throw new ResolutionFailedException(resolution.Outcome);
        }

        var musicCatalogId = resolution.MusicCatalogId ?? throw new ResolutionFailedException(resolution.Outcome);
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

        var localTrack = await localMusicTrackSearch.GetByMusicCatalogIdAsync(musicCatalogId, cancellationToken);
        var command = BuildCommand(request, musicCatalogId, plan.Priority!.Value, localTrack);
        if (command is null)
        {
            return LookupSchedulingResult.DoNotSchedule();
        }

        var acquired = await activeLookupWorkStore.TryAcquireAsync(
            command.CommandId,
            request.OccurredAt.Add(ActiveReservationDuration),
            cancellationToken);

        return acquired
            ? LookupSchedulingResult.Schedule(command)
            : LookupSchedulingResult.DoNotSchedule();
    }

    private static LookupPhaseCommand? BuildCommand(
        LookupMusicRequest request,
        MusicCatalogId musicCatalogId,
        LookupPriorityBand priority,
        LocalMusicTrackSearchResult? localTrack)
    {
        if (localTrack?.IsPlayable == true)
        {
            return null;
        }

        var searchTerm = localTrack?.GetSearchTerm();
        if ((localTrack != null ? !string.IsNullOrWhiteSpace(localTrack.Isrc) : null) == true && searchTerm is not null)
        {
            return new ResolvePlaybackReferencesCommand(
                CommandId.For($"ResolvePlaybackReferences:{musicCatalogId.Value}"),
                musicCatalogId,
                priority,
                request.OccurredAt,
                request.CorrelationId,
                searchTerm);
        }

        if (searchTerm is null)
        {
            return null;
        }

        return new LookupCanonicalMusicMetadataCommand(
            CommandId.For($"LookupCanonicalMusicMetadata:{musicCatalogId.Value}"),
            musicCatalogId,
            priority,
            request.OccurredAt,
            request.CorrelationId,
            searchTerm);
    }
}
