using Raven.Client.Documents;
using Soundtrail.Contracts.Persistence;
using Soundtrail.Domain.Catalog.Playlists;
using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Events;
using Soundtrail.Services.Internal.Projector.Features.OnWorkFeedbackChanged.Adapters;
using Soundtrail.Services.Tests.Integration.Shared.Infrastructure;

namespace Soundtrail.Services.Tests.Integration.Shared.Projector.StoreDiscoveryFeedback;

internal sealed class StoreDiscoveryFeedbackPortContractTestEnvironment : IAsyncDisposable
{
    private readonly IDocumentStore? documentStore;
    private readonly StoreDiscoveryFeedbackPortContractFake? fake;
    private readonly List<string> cleanupDocumentIds = [];

    private StoreDiscoveryFeedbackPortContractTestEnvironment(
        IStoreDiscoveryFeedbackPort subject,
        IDocumentStore? documentStore,
        StoreDiscoveryFeedbackPortContractFake? fake)
    {
        Subject = subject;
        this.documentStore = documentStore;
        this.fake = fake;
    }

    public IStoreDiscoveryFeedbackPort Subject { get; }

    public static StoreDiscoveryFeedbackPortContractTestEnvironment Create(
        StoreDiscoveryFeedbackPortImplementation implementation) =>
        implementation switch
        {
            StoreDiscoveryFeedbackPortImplementation.Fake => CreateFake(),
            StoreDiscoveryFeedbackPortImplementation.Raven => CreateRaven(),
            _ => throw new ArgumentOutOfRangeException(nameof(implementation), implementation, null)
        };

    public EnrichmentTarget PlaylistTarget(string playlistName = "world_top_100") =>
        Work.DiscoverPlaylistTracks(PlaylistId.FromPlaylistName(playlistName));

    public EnrichmentTarget StreamingTarget(TrackId trackId) =>
        Work.EnrichTrackStreamingLocation(trackId);

    public async Task SeedPlaylistAsync(CatalogPlaylistTracksRecordDto playlist)
    {
        cleanupDocumentIds.Add(playlist.Id);
        if (fake is not null)
        {
            fake.SeedPlaylist(playlist);
            return;
        }

        using var session = documentStore!.OpenAsyncSession();
        await session.StoreAsync(playlist, playlist.Id);
        await session.SaveChangesAsync();
    }

    public async Task<CatalogDiscoveryFeedbackRecordDto?> LoadFeedbackAsync(EnrichmentTarget target)
    {
        cleanupDocumentIds.Add(CatalogDiscoveryFeedbackRecordDto.GetDocumentId(target.NormalisedIdentifier));

        if (fake is not null)
        {
            return fake.LoadFeedback(target.NormalisedIdentifier);
        }

        using var session = documentStore!.OpenAsyncSession();
        return await session.LoadAsync<CatalogDiscoveryFeedbackRecordDto>(
            CatalogDiscoveryFeedbackRecordDto.GetDocumentId(target.NormalisedIdentifier));
    }

    public async Task<CatalogPlaylistTracksRecordDto?> LoadPlaylistAsync(string playlistId)
    {
        var documentId = CatalogPlaylistTracksRecordDto.GetDocumentId(playlistId);
        cleanupDocumentIds.Add(documentId);

        if (fake is not null)
        {
            return fake.LoadPlaylist(playlistId);
        }

        using var session = documentStore!.OpenAsyncSession();
        return await session.LoadAsync<CatalogPlaylistTracksRecordDto>(documentId);
    }

    public async ValueTask DisposeAsync()
    {
        await EmbeddedRavenTestServer.DisposeAsync(documentStore);
    }

    private static StoreDiscoveryFeedbackPortContractTestEnvironment CreateFake()
    {
        var fake = new StoreDiscoveryFeedbackPortContractFake();
        return new StoreDiscoveryFeedbackPortContractTestEnvironment(fake, null, fake);
    }

    private static StoreDiscoveryFeedbackPortContractTestEnvironment CreateRaven()
    {
        var store = EmbeddedRavenTestServer.CreateDocumentStore();
        return new StoreDiscoveryFeedbackPortContractTestEnvironment(
            new RavenStoreDiscoveryFeedbackPort(store),
            store,
            fake: null);
    }
}

/// <summary>
/// Contract fake that mirrors Raven write-time embed of discovery onto playlist docs.
/// </summary>
internal sealed class StoreDiscoveryFeedbackPortContractFake : IStoreDiscoveryFeedbackPort
{
    private readonly Dictionary<string, CatalogDiscoveryFeedbackRecordDto> feedback = new(StringComparer.Ordinal);
    private readonly Dictionary<string, CatalogPlaylistTracksRecordDto> playlists = new(StringComparer.Ordinal);

    public void SeedPlaylist(CatalogPlaylistTracksRecordDto playlist) =>
        playlists[playlist.PlaylistId] = playlist;

    public CatalogDiscoveryFeedbackRecordDto? LoadFeedback(string targetId) =>
        feedback.GetValueOrDefault(CatalogDiscoveryFeedbackRecordDto.GetDocumentId(targetId))
        ?? feedback.Values.FirstOrDefault(record => record.TargetId == targetId);

    public CatalogPlaylistTracksRecordDto? LoadPlaylist(string playlistId) =>
        playlists.GetValueOrDefault(playlistId);

    public Task StoreAsync(WorkRequested @event, CancellationToken cancellationToken) =>
        Upsert(@event.Target.NormalisedIdentifier, "requested", @event.Priority, null, null, string.Empty, @event.RequestedAt);

    public Task StoreAsync(WorkScheduled @event, CancellationToken cancellationToken) =>
        Upsert(@event.Target.NormalisedIdentifier, "scheduled", @event.Priority, @event.NextEligibleAt, @event.EarliestExpectedCompletionAt, @event.Reason, @event.ScheduledAt);

    public Task StoreAsync(WorkDeferred @event, CancellationToken cancellationToken) =>
        Upsert(@event.Target.NormalisedIdentifier, "deferred", @event.Priority, @event.NextEligibleAt, null, @event.Reason, @event.DeferredAt);

    public Task StoreAsync(WorkCompleted @event, CancellationToken cancellationToken) =>
        Upsert(@event.Target.NormalisedIdentifier, "completed", @event.Priority, null, null, @event.Reason, @event.CompletedAt);

    public Task StoreAsync(WorkRejected @event, CancellationToken cancellationToken) =>
        Upsert(@event.Target.NormalisedIdentifier, "rejected", @event.Priority, null, null, @event.Reason, @event.RejectedAt);

    public Task StoreAsync(WorkIgnored @event, CancellationToken cancellationToken) =>
        Upsert(@event.Target.NormalisedIdentifier, "ignored", @event.Priority, @event.NextEligibleAt, @event.EarliestExpectedCompletionAt, @event.Reason, @event.IgnoredAt);

    public Task StoreAsync(WorkAttemptFailed @event, CancellationToken cancellationToken)
    {
        var id = CatalogDiscoveryFeedbackRecordDto.GetDocumentId(@event.Target.NormalisedIdentifier);
        if (feedback.TryGetValue(id, out var existing) && existing.Status == "completed")
        {
            return Task.CompletedTask;
        }

        var record = existing ?? new CatalogDiscoveryFeedbackRecordDto
        {
            Id = id,
            TargetId = @event.Target.NormalisedIdentifier
        };
        record.Status = "attempt-failed";
        record.Reason = @event.Reason;
        record.UpdatedAtUtc = @event.FailedAt;
        feedback[id] = record;
        Embed(@event.Target.NormalisedIdentifier, record);
        return Task.CompletedTask;
    }

    private Task Upsert(
        string targetId,
        string status,
        LookupPriorityBand priority,
        DateTimeOffset? nextEligibleAt,
        DateTimeOffset? earliestExpectedCompletionAt,
        string reason,
        DateTimeOffset updatedAt)
    {
        var id = CatalogDiscoveryFeedbackRecordDto.GetDocumentId(targetId);
        var record = feedback.GetValueOrDefault(id) ?? new CatalogDiscoveryFeedbackRecordDto
        {
            Id = id,
            TargetId = targetId
        };
        record.Status = status;
        record.Priority = priority.ToString();
        record.NextEligibleAtUtc = nextEligibleAt;
        record.EarliestExpectedCompletionAtUtc = earliestExpectedCompletionAt;
        record.Reason = reason;
        record.UpdatedAtUtc = updatedAt;
        feedback[id] = record;
        Embed(targetId, record);
        return Task.CompletedTask;
    }

    private void Embed(string targetId, CatalogDiscoveryFeedbackRecordDto discovery)
    {
        const string playlistPrefix = "child_tracks_for_playlist:";
        if (targetId.StartsWith(playlistPrefix, StringComparison.Ordinal))
        {
            var playlistId = targetId[playlistPrefix.Length..];
            if (playlists.TryGetValue(playlistId, out var playlist))
            {
                playlist.Discovery = BuildPlaylistEndpointDiscovery(playlist, discovery);
            }

            return;
        }

        const string streamingPrefix = "streaming_location_for_track:";
        if (!targetId.StartsWith(streamingPrefix, StringComparison.Ordinal))
        {
            return;
        }

        var trackId = TrackId.From(targetId[streamingPrefix.Length..]);
        foreach (var playlist in playlists.Values.Where(record => ContainsSameBaseTrack(record, trackId)))
        {
            playlist.Discovery = BuildPlaylistEndpointDiscovery(playlist, discovery);
        }
    }

    private CatalogDiscoveryFeedbackRecordDto BuildPlaylistEndpointDiscovery(
        CatalogPlaylistTracksRecordDto playlist,
        CatalogDiscoveryFeedbackRecordDto fallbackDiscovery)
    {
        var playlistTargetId = $"child_tracks_for_playlist:{playlist.PlaylistId}";
        var playlistDiscovery = fallbackDiscovery.TargetId == playlistTargetId
            ? fallbackDiscovery
            : LoadFeedback(playlistTargetId);

        if (playlistDiscovery is null)
        {
            return EmbedCopy(fallbackDiscovery);
        }

        foreach (var track in playlist.Tracks.Where(static track => track.StreamingLocations.Length == 0))
        {
            var streamingTargetId = $"streaming_location_for_track:{track.TrackId}";
            var streamingDiscovery = fallbackDiscovery.TargetId == streamingTargetId
                ? fallbackDiscovery
                : LoadFeedback(streamingTargetId);

            if (streamingDiscovery is null)
            {
                return BuildStreamingPending(playlistDiscovery);
            }

            if (streamingDiscovery.Status is "requested" or "scheduled" or "deferred" or "attempt-failed")
            {
                return EmbedCopy(streamingDiscovery);
            }

            if (streamingDiscovery.Status == "completed"
                && string.Equals(streamingDiscovery.Reason, "Lookup completed.", StringComparison.Ordinal))
            {
                return BuildStreamingPending(playlistDiscovery);
            }
        }

        return EmbedCopy(playlistDiscovery);
    }

    private static CatalogDiscoveryFeedbackRecordDto BuildStreamingPending(CatalogDiscoveryFeedbackRecordDto playlistDiscovery)
    {
        var pending = EmbedCopy(playlistDiscovery);
        pending.Status = "scheduled";
        pending.NextEligibleAtUtc = playlistDiscovery.UpdatedAtUtc.AddSeconds(15);
        pending.EarliestExpectedCompletionAtUtc = playlistDiscovery.UpdatedAtUtc.AddSeconds(75);
        pending.Reason = "Track streaming projection is still catching up.";
        return pending;
    }

    private static CatalogDiscoveryFeedbackRecordDto EmbedCopy(CatalogDiscoveryFeedbackRecordDto discovery) =>
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

    private static bool ContainsSameBaseTrack(CatalogPlaylistTracksRecordDto record, TrackId trackId)
    {
        var requested = TrackIdIndexProjection.From(trackId);
        return record.TrackIds.Concat(record.Tracks.Select(static track => track.TrackId))
            .Select(TrackId.From)
            .Select(TrackIdIndexProjection.From)
            .Any(existing => existing.SharesBaseWith(requested));
    }
}

public enum StoreDiscoveryFeedbackPortImplementation
{
    Fake,
    Raven
}
