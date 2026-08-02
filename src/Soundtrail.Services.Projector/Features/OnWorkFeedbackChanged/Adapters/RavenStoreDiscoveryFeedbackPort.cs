using Raven.Client.Documents;
using Raven.Client.Documents.Session;
using Soundtrail.Contracts.Persistence;
using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery.Events;

namespace Soundtrail.Services.Internal.Projector.Features.OnWorkFeedbackChanged.Adapters;

public sealed class RavenStoreDiscoveryFeedbackPort(
    IDocumentStore documentStore) : IStoreDiscoveryFeedbackPort
{
    public Task StoreAsync(WorkRequested @event, CancellationToken cancellationToken) =>
        UpsertAsync(
            @event.Target.NormalisedIdentifier,
            "requested",
            @event.Priority,
            nextEligibleAtUtc: null,
            earliestExpectedCompletionAtUtc: null,
            reason: string.Empty,
            updatedAtUtc: @event.RequestedAt,
            cancellationToken);

    public Task StoreAsync(WorkScheduled @event, CancellationToken cancellationToken) =>
        UpsertAsync(
            @event.Target.NormalisedIdentifier,
            "scheduled",
            @event.Priority,
            @event.NextEligibleAt,
            @event.EarliestExpectedCompletionAt,
            @event.Reason,
            @event.ScheduledAt,
            cancellationToken);

    public Task StoreAsync(WorkDeferred @event, CancellationToken cancellationToken) =>
        UpsertAsync(
            @event.Target.NormalisedIdentifier,
            "deferred",
            @event.Priority,
            @event.NextEligibleAt,
            earliestExpectedCompletionAtUtc: null,
            @event.Reason,
            @event.DeferredAt,
            cancellationToken);

    public Task StoreAsync(WorkCompleted @event, CancellationToken cancellationToken) =>
        UpsertAsync(
            @event.Target.NormalisedIdentifier,
            "completed",
            @event.Priority,
            nextEligibleAtUtc: null,
            earliestExpectedCompletionAtUtc: null,
            @event.Reason,
            @event.CompletedAt,
            cancellationToken);

    public Task StoreAsync(WorkRejected @event, CancellationToken cancellationToken) =>
        UpsertAsync(
            @event.Target.NormalisedIdentifier,
            "rejected",
            @event.Priority,
            nextEligibleAtUtc: null,
            earliestExpectedCompletionAtUtc: null,
            @event.Reason,
            @event.RejectedAt,
            cancellationToken);

    public Task StoreAsync(WorkIgnored @event, CancellationToken cancellationToken) =>
        UpsertAsync(
            @event.Target.NormalisedIdentifier,
            "ignored",
            @event.Priority,
            @event.NextEligibleAt,
            @event.EarliestExpectedCompletionAt,
            @event.Reason,
            @event.IgnoredAt,
            cancellationToken);

    public async Task StoreAsync(WorkAttemptFailed @event, CancellationToken cancellationToken)
    {
        using var session = documentStore.OpenAsyncSession();
        var id = CatalogDiscoveryFeedbackRecordDto.GetDocumentId(@event.Target.NormalisedIdentifier);
        var record = await session.LoadAsync<CatalogDiscoveryFeedbackRecordDto>(id, cancellationToken)
            ?? CreateRecord(@event.Target.NormalisedIdentifier);

        if (record.Status == "completed")
        {
            return;
        }

        record.Status = "attempt-failed";
        record.Reason = @event.Reason;
        record.UpdatedAtUtc = @event.FailedAt;

        await UpdateEndpointProjectionAsync(session, @event.Target.NormalisedIdentifier, record, cancellationToken);
        await session.StoreAsync(record, cancellationToken);
        await session.SaveChangesAsync(cancellationToken);
    }

    private async Task UpsertAsync(
        string targetId,
        string status,
        LookupPriorityBand priority,
        DateTimeOffset? nextEligibleAtUtc,
        DateTimeOffset? earliestExpectedCompletionAtUtc,
        string reason,
        DateTimeOffset updatedAtUtc,
        CancellationToken cancellationToken)
    {
        using var session = documentStore.OpenAsyncSession();
        var id = CatalogDiscoveryFeedbackRecordDto.GetDocumentId(targetId);
        var record = await session.LoadAsync<CatalogDiscoveryFeedbackRecordDto>(id, cancellationToken)
            ?? CreateRecord(targetId);

        record.Status = status;
        record.Priority = priority.ToString();
        record.NextEligibleAtUtc = nextEligibleAtUtc;
        record.EarliestExpectedCompletionAtUtc = earliestExpectedCompletionAtUtc;
        record.Reason = reason;
        record.UpdatedAtUtc = updatedAtUtc;

        await UpdateEndpointProjectionAsync(session, targetId, record, cancellationToken);
        await session.StoreAsync(record, cancellationToken);
        await session.SaveChangesAsync(cancellationToken);
    }

    private static CatalogDiscoveryFeedbackRecordDto CreateRecord(string targetId) =>
        new()
        {
            Id = CatalogDiscoveryFeedbackRecordDto.GetDocumentId(targetId),
            TargetId = targetId,
            Priority = LookupPriorityBand.Low.ToString()
        };

    private static async Task UpdateEndpointProjectionAsync(
        IAsyncDocumentSession session,
        string targetId,
        CatalogDiscoveryFeedbackRecordDto discovery,
        CancellationToken cancellationToken)
    {
        if (TryGetPlaylistId(targetId, out var playlistId))
        {
            var playlistRecord = await session.LoadAsync<CatalogPlaylistTracksRecordDto>(
                CatalogPlaylistTracksRecordDto.GetDocumentId(playlistId),
                cancellationToken);

            if (playlistRecord is not null)
            {
                playlistRecord.Discovery = await BuildPlaylistEndpointDiscoveryAsync(
                    session,
                    playlistRecord,
                    fallbackDiscovery: discovery,
                    cancellationToken);
            }

            return;
        }

        if (TryGetStreamingTrackId(targetId, out var trackId) is false)
        {
            return;
        }

        var playlistRecords = await session.Advanced.LoadStartingWithAsync<CatalogPlaylistTracksRecordDto>(
            "catalog/playlist-tracks/",
            token: cancellationToken);
        var affectedRecords = playlistRecords
            .Where(record => ContainsSameBaseTrack(record, trackId))
            .ToArray();

        foreach (var playlistRecord in affectedRecords)
        {
            playlistRecord.Discovery = await BuildPlaylistEndpointDiscoveryAsync(
                session,
                playlistRecord,
                fallbackDiscovery: discovery,
                cancellationToken);
        }
    }

    private static bool TryGetPlaylistId(string targetId, out string playlistId)
    {
        const string prefix = "child_tracks_for_playlist:";

        if (targetId.StartsWith(prefix, StringComparison.Ordinal) is false)
        {
            playlistId = string.Empty;
            return false;
        }

        playlistId = targetId[prefix.Length..];
        return playlistId.Length > 0;
    }

    private static bool TryGetStreamingTrackId(string targetId, out TrackId trackId)
    {
        const string prefix = "streaming_location_for_track:";

        if (targetId.StartsWith(prefix, StringComparison.Ordinal) is false)
        {
            trackId = default;
            return false;
        }

        trackId = TrackId.From(targetId[prefix.Length..]);
        return true;
    }

    private static async Task<CatalogDiscoveryFeedbackRecordDto> BuildPlaylistEndpointDiscoveryAsync(
        IAsyncDocumentSession session,
        CatalogPlaylistTracksRecordDto playlist,
        CatalogDiscoveryFeedbackRecordDto fallbackDiscovery,
        CancellationToken cancellationToken)
    {
        var playlistTargetId = $"child_tracks_for_playlist:{playlist.PlaylistId}";
        var playlistDiscovery = fallbackDiscovery.TargetId == playlistTargetId
            ? fallbackDiscovery
            : await session.LoadAsync<CatalogDiscoveryFeedbackRecordDto>(
                CatalogDiscoveryFeedbackRecordDto.GetDocumentId(playlistTargetId),
                cancellationToken);

        if (playlistDiscovery is null)
        {
            return EmbedDiscovery(fallbackDiscovery);
        }

        foreach (var track in playlist.Tracks.Where(static track => track.StreamingLocations.Length == 0))
        {
            var streamingTargetId = $"streaming_location_for_track:{track.TrackId}";
            var streamingDiscovery = fallbackDiscovery.TargetId == streamingTargetId
                ? fallbackDiscovery
                : await session.LoadAsync<CatalogDiscoveryFeedbackRecordDto>(
                    CatalogDiscoveryFeedbackRecordDto.GetDocumentId(streamingTargetId),
                    cancellationToken);

            if (streamingDiscovery is null || IsIncomplete(streamingDiscovery))
            {
                return streamingDiscovery is null
                    ? BuildStreamingProjectionPendingDiscovery(playlistDiscovery)
                    : EmbedDiscovery(streamingDiscovery);
            }
        }

        return EmbedDiscovery(playlistDiscovery);
    }

    private static bool IsIncomplete(CatalogDiscoveryFeedbackRecordDto discovery) =>
        discovery.Status is "requested" or "scheduled" or "deferred";

    private static CatalogDiscoveryFeedbackRecordDto BuildStreamingProjectionPendingDiscovery(
        CatalogDiscoveryFeedbackRecordDto playlistDiscovery)
    {
        var pending = EmbedDiscovery(playlistDiscovery);
        pending.Status = "scheduled";
        pending.NextEligibleAtUtc = playlistDiscovery.UpdatedAtUtc.AddSeconds(15);
        pending.EarliestExpectedCompletionAtUtc = playlistDiscovery.UpdatedAtUtc.AddSeconds(75);
        pending.Reason = "Track streaming projection is still catching up.";
        return pending;
    }

    private static bool ContainsSameBaseTrack(CatalogPlaylistTracksRecordDto record, TrackId trackId)
    {
        var requestedProjection = TrackIdIndexProjection.From(trackId);
        return record.TrackIds.Concat(record.Tracks.Select(static track => track.TrackId))
            .Select(TrackId.From)
            .Select(TrackIdIndexProjection.From)
            .Any(existingProjection => existingProjection.SharesBaseWith(requestedProjection));
    }

    private static CatalogDiscoveryFeedbackRecordDto EmbedDiscovery(CatalogDiscoveryFeedbackRecordDto discovery) =>
        new()
        {
            TargetId = discovery.TargetId,
            Status = discovery.Status,
            Priority = discovery.Priority,
            NextEligibleAtUtc = discovery.NextEligibleAtUtc,
            EarliestExpectedCompletionAtUtc = discovery.EarliestExpectedCompletionAtUtc,
            Reason = discovery.Reason,
            UpdatedAtUtc = discovery.UpdatedAtUtc
        };
}
