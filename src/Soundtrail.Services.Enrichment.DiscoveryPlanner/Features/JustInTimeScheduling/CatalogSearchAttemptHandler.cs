using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Idempotency;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Persistence;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Discovery;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Prioritisation;
using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Commands;
using Soundtrail.Domain.Responses;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.JustInTimeScheduling.Model;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Search;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Search.Resolution;

namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.JustInTimeScheduling;

public sealed class CatalogSearchAttemptHandler(
    IMusicCatalogCandidateSearch musicCatalogCandidateSearch,
    IPotentialCatalogLookupWorkStore potentialCatalogLookupWorkStore,
    ICatalogSearchTrackingStore catalogSearchTrackingStore,
    DiscoveryPriorityPolicy discoveryPriorityPolicy,
    MusicCatalogMatchResolver musicCatalogMatchResolver,
    IActiveLookupWorkStore activeLookupWorkStore,
    ILocalMusicTrackSearch localMusicTrackSearch)
{
    private static readonly TimeSpan ActiveReservationDuration = TimeSpan.FromMinutes(15);

    public Task<LookupSchedulingResult> Handle(
        CatalogSearchAttempt request,
        CancellationToken cancellationToken = default) =>
        ScheduleAsync(request, cancellationToken);

    public async Task<LookupSchedulingResult> ScheduleAsync(
        CatalogSearchAttempt request,
        CancellationToken cancellationToken = default)
    {
        var matches = await musicCatalogCandidateSearch.SearchAsync(request.Query, cancellationToken);
        var resolution = musicCatalogMatchResolver.Resolve(matches);
        if (!resolution.IsResolved)
        {
            throw new ResolutionFailedException(resolution.Outcome);
        }

        var musicCatalogId = resolution.MusicCatalogId ?? throw new ResolutionFailedException(resolution.Outcome);
        var existing = await potentialCatalogLookupWorkStore.FindByMusicCatalogIdAsync(musicCatalogId, cancellationToken);
        var rankedMusicCandidate = existing is null
            ? PotentialCatalogLookupWork.Create(request, musicCatalogId)
            : existing.AcceptNewRequest(request);
        await potentialCatalogLookupWorkStore.UpsertAsync(rankedMusicCandidate, cancellationToken);
        await catalogSearchTrackingStore.UpsertAsync(
            new CatalogSearchTracking(
                request.Criteria,
                musicCatalogId,
                request.OccurredAt),
            cancellationToken);

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
        CatalogSearchAttempt request,
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
                searchTerm,
                ToHierarchy(localTrack));
        }

        if (searchTerm is null)
        {
            return null;
        }

        return new LookupMusicMetadataCommand(
            CommandId.For($"LookupCanonicalMusicMetadata:{musicCatalogId.Value}"),
            musicCatalogId,
            priority,
            request.OccurredAt,
            request.CorrelationId,
            searchTerm,
            ToHierarchy(localTrack));
    }

    private static CatalogTrackHierarchy? ToHierarchy(LocalMusicTrackSearchResult? localTrack) =>
        localTrack?.ArtistId is null && localTrack?.AlbumId is null
            ? null
            : new CatalogTrackHierarchy(localTrack?.ArtistId, localTrack?.AlbumId);
}
