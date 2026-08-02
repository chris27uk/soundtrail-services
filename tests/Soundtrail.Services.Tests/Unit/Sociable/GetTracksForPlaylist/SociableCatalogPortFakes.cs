using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Catalog.Playlists;
using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Candidates;
using Soundtrail.Domain.Discovery.Events;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForPlaylist.Contract;
using Soundtrail.Services.Api.Features.Catalog.Shared.Contract;
using Soundtrail.Services.Enrichment.Worker.Shared.StreamingLocations;
using Soundtrail.Services.Internal.Projector.Features.OnArtistCatalogChanged;
using Soundtrail.Services.Internal.Projector.Features.OnArtistCatalogChanged.Adapters;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchCandidateChanged;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchCandidateChanged.Adapters;
using Soundtrail.Services.Internal.Projector.Features.OnPlaylistTracksDiscovered.Adapters;
using Soundtrail.Services.Internal.Projector.Features.OnWorkFeedbackChanged.Adapters;
using Soundtrail.Services.Tests.Fakes;

namespace Soundtrail.Services.Tests.Unit.Sociable.GetTracksForPlaylist;

internal sealed class StoreDiscoveryFeedbackPortFake : IStoreDiscoveryFeedbackPort
{
    private readonly Dictionary<string, DiscoveryFeedbackResponse> feedback = new(StringComparer.Ordinal);

    public DiscoveryFeedbackResponse? Read(string targetId) => feedback.GetValueOrDefault(targetId);

    public Task StoreAsync(WorkRequested @event, CancellationToken cancellationToken) =>
        Store(@event.Target, "requested", @event.Priority, null, null, string.Empty, @event.RequestedAt);

    public Task StoreAsync(WorkScheduled @event, CancellationToken cancellationToken) =>
        Store(@event.Target, "scheduled", @event.Priority, @event.NextEligibleAt, @event.EarliestExpectedCompletionAt, @event.Reason, @event.ScheduledAt);

    public Task StoreAsync(WorkDeferred @event, CancellationToken cancellationToken) =>
        Store(@event.Target, "deferred", @event.Priority, @event.NextEligibleAt, null, @event.Reason, @event.DeferredAt);

    public Task StoreAsync(WorkCompleted @event, CancellationToken cancellationToken) =>
        Store(@event.Target, "completed", @event.Priority, null, null, @event.Reason, @event.CompletedAt);

    public Task StoreAsync(WorkRejected @event, CancellationToken cancellationToken) =>
        Store(@event.Target, "rejected", @event.Priority, null, null, @event.Reason, @event.RejectedAt);

    public Task StoreAsync(WorkIgnored @event, CancellationToken cancellationToken) =>
        Store(@event.Target, "ignored", @event.Priority, @event.NextEligibleAt, @event.EarliestExpectedCompletionAt, @event.Reason, @event.IgnoredAt);

    public Task StoreAsync(WorkAttemptFailed @event, CancellationToken cancellationToken)
    {
        var existing = Read(@event.Target.NormalisedIdentifier);
        if (existing?.Status == "completed")
        {
            return Task.CompletedTask;
        }

        feedback[@event.Target.NormalisedIdentifier] = (existing ?? new DiscoveryFeedbackResponse(
            string.Empty,
            LookupPriorityBand.Low,
            null,
            null,
            string.Empty,
            @event.FailedAt)) with
        {
            Status = "attempt-failed",
            Reason = @event.Reason,
            UpdatedAtUtc = @event.FailedAt
        };
        return Task.CompletedTask;
    }

    private Task Store(
        EnrichmentTarget target,
        string status,
        LookupPriorityBand priority,
        DateTimeOffset? nextEligibleAt,
        DateTimeOffset? earliestExpectedCompletionAt,
        string reason,
        DateTimeOffset updatedAt)
    {
        feedback[target.NormalisedIdentifier] = new DiscoveryFeedbackResponse(
            status,
            priority,
            nextEligibleAt,
            earliestExpectedCompletionAt,
            reason,
            updatedAt);
        return Task.CompletedTask;
    }
}

internal sealed class StoreArtistCatalogReadModelPortFake(
    ReadTrackForLookupPortFake readTrackForLookup) : IStoreArtistCatalogReadModelPort
{
    private readonly Dictionary<TrackId, (ArtistId ArtistId, ArtistCatalogTrackReadModel Track, DateTimeOffset UpdatedAt)> tracks = [];

    public IReadOnlyCollection<(ArtistId ArtistId, ArtistCatalogTrackReadModel Track, DateTimeOffset UpdatedAt)> Tracks => tracks.Values;

    public Task StoreAsync(ArtistCatalogReadModel readModel, CancellationToken cancellationToken)
    {
        foreach (var track in readModel.Tracks)
        {
            tracks[track.TrackId] = (readModel.ArtistId, track, readModel.UpdatedAt);
            readTrackForLookup.WithTrack(new TrackLookupContext(
                readModel.ArtistId,
                track.TrackId,
                track.Title,
                track.ArtistName,
                track.Isrc));
        }

        return Task.CompletedTask;
    }

    public TrackLookupContext? Read(TrackId trackId) =>
        tracks.TryGetValue(trackId, out var stored)
            ? new TrackLookupContext(stored.ArtistId, trackId, stored.Track.Title, stored.Track.ArtistName, stored.Track.Isrc)
            : null;
}

internal sealed class StorePlaylistTracksReadModelPortFake(
    ClockFake clock,
    StoreArtistCatalogReadModelPortFake artistCatalog,
    StoreDiscoveryFeedbackPortFake discoveryFeedback) : IStorePlaylistTracksReadModelPort
{
    private readonly Dictionary<PlaylistId, (TrackId[] TrackIds, DateTimeOffset UpdatedAt)> playlists = [];

    public Task StoreAsync(PlaylistTracksDiscovered @event, CancellationToken cancellationToken)
    {
        var existing = playlists.GetValueOrDefault(@event.PlaylistId).TrackIds ?? [];
        playlists[@event.PlaylistId] = (
            existing.Concat(@event.Tracks).Distinct().ToArray(),
            @event.ObservedAt);
        return Task.CompletedTask;
    }

    public Task RepairTrackAsync(TrackId trackId, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task<GetTracksForPlaylistResponse?> ReadAsync(PlaylistId playlistId, CancellationToken cancellationToken)
    {
        if (!playlists.TryGetValue(playlistId, out var playlist))
        {
            return Task.FromResult<GetTracksForPlaylistResponse?>(null);
        }

        var tracks = playlist.TrackIds
            .Select(SelectPreferredTrack)
            .Where(static track => track is not null)
            .Select(static track => track!.Value)
            .Select(static track => new GetTracksForPlaylistTrackResponse(
                track.Track.TrackId,
                track.Track.Title,
                track.Track.ArtistName,
                track.Track.AlbumTitle,
                track.Track.DurationMs,
                track.Track.Isrc,
                track.Track.ReleaseDate,
                track.Track.ArtworkUrl,
                track.Track.StreamingLocations.Length > 0,
                track.Track.StreamingLocations
                    .Select(static location => new StreamingLocationResponse(
                        location.Provider.StableValue,
                        location.ExternalId,
                        location.Url))
                    .ToArray()))
            .ToArray();

        return Task.FromResult<GetTracksForPlaylistResponse?>(new GetTracksForPlaylistResponse(
            playlistId,
            tracks,
            ReadDiscovery(playlistId, tracks)));
    }

    private (ArtistId ArtistId, ArtistCatalogTrackReadModel Track, DateTimeOffset UpdatedAt)? SelectPreferredTrack(TrackId requestedTrackId)
    {
        var requested = TrackIdIndexProjection.From(requestedTrackId);
        return artistCatalog.Tracks
            .Select(track => (Stored: track, Projection: TrackIdIndexProjection.From(track.Track.TrackId)))
            .Where(track => track.Projection.SharesBaseWith(requested))
            .OrderBy(track => track.Projection.GetDistanceTo(requested))
            .ThenByDescending(static track => track.Stored.UpdatedAt)
            .Select(static track => ((ArtistId, ArtistCatalogTrackReadModel, DateTimeOffset)?)track.Stored)
            .FirstOrDefault();
    }

    private DiscoveryFeedbackResponse? ReadDiscovery(
        PlaylistId playlistId,
        IReadOnlyList<GetTracksForPlaylistTrackResponse> tracks)
    {
        var playlistDiscovery = discoveryFeedback.Read($"child_tracks_for_playlist:{playlistId.Value}");
        if (playlistDiscovery is null)
        {
            return null;
        }

        foreach (var track in tracks.Where(static track => !track.Playable))
        {
            var streamingDiscovery = discoveryFeedback.Read($"streaming_location_for_track:{track.TrackId.Value}");
            if (streamingDiscovery is not null && streamingDiscovery.Status is "requested" or "scheduled" or "deferred")
            {
                return streamingDiscovery;
            }

            if (streamingDiscovery is null)
            {
                return playlistDiscovery with
                {
                    Status = "scheduled",
                    NextEligibleAt = clock.UtcNow.AddSeconds(15),
                    EarliestExpectedCompletionAt = clock.UtcNow.AddSeconds(75),
                    Reason = "Track streaming projection is still catching up.",
                    UpdatedAtUtc = clock.UtcNow
                };
            }
        }

        return playlistDiscovery;
    }
}

internal sealed class StoreCatalogSearchCandidatePortFake : IStoreCatalogSearchCandidatePort
{
    private readonly Dictionary<string, CatalogSearchCandidateProjection> candidates = new(StringComparer.Ordinal);

    public IReadOnlyCollection<CatalogSearchCandidateProjection> Candidates => candidates.Values;

    public Task StoreAsync(CatalogSearchCandidateProjection projection, CancellationToken cancellationToken)
    {
        candidates[projection.CatalogItemId] = projection;
        return Task.CompletedTask;
    }
}

internal sealed class SearchForCandidatesFake(StoreCatalogSearchCandidatePortFake searchCandidates) : ISearchForCandidates
{
    public CandidatesResult Search(EnrichmentTarget target)
    {
        if (target is not EnrichmentTarget.SearchForUnknownCatalogItem(var searchCriteria))
        {
            return new CandidatesResult.None();
        }

        var normalized = searchCriteria.NormalisedIdentifier["search:".Length..];
        var matches = searchCandidates.Candidates
            .Where(candidate => candidate.CandidateKind == "track")
            .Where(candidate => StringNormalizationExtensions.Normalize(candidate.SearchText) == normalized)
            .Select(candidate => new ScoredCandidate(new CatalogItemId.Track(TrackId.From(candidate.CatalogItemId)), 100))
            .ToList();
        return matches.Count == 0
            ? new CandidatesResult.None()
            : new CandidatesResult.Results(CandidateList.From(matches));
    }
}
