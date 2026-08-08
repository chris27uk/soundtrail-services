using Soundtrail.Domain.Common;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForPlaylist.Contract;
using Soundtrail.Services.Api.Features.Catalog.Shared.Contract;

namespace Soundtrail.Services.Api.Features.Catalog.GetTracksForPlaylist.Discovery;

internal sealed class PlaylistTracksDiscoveryFeedback
{
    private static readonly TimeSpan PendingRetryDelay = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan PendingCompletionDelay = TimeSpan.FromSeconds(75);

    private readonly DiscoveryFeedbackResponse? projectedDiscovery;
    private readonly bool projectionContainsTracks;

    private PlaylistTracksDiscoveryFeedback(
        DiscoveryFeedbackResponse? projectedDiscovery,
        bool projectionContainsTracks)
    {
        this.projectedDiscovery = projectedDiscovery;
        this.projectionContainsTracks = projectionContainsTracks;
    }

    public static PlaylistTracksDiscoveryFeedback MissingProjection() =>
        new(projectedDiscovery: null, projectionContainsTracks: false);

    public static PlaylistTracksDiscoveryFeedback FromProjection(GetTracksForPlaylistResponse response) =>
        new(response.Discovery, response.Tracks.Length > 0);

    public DiscoveryFeedbackResponse EstimateAt(DateTimeOffset requestedAt)
    {
        if (this.projectedDiscovery is null)
        {
            return PlaylistLookupQueued(requestedAt);
        }

        if (this.projectionContainsTracks is false && this.projectedDiscovery.Status == "completed")
        {
            return PlaylistProjectionCatchingUp(requestedAt);
        }

        return this.projectedDiscovery;
    }

    private static DiscoveryFeedbackResponse PlaylistLookupQueued(DateTimeOffset requestedAt) =>
        new(
            "scheduled",
            LookupPriorityBand.High,
            requestedAt.Add(PendingRetryDelay),
            requestedAt.Add(PendingCompletionDelay),
            "Playlist lookup queued.",
            requestedAt);

    private DiscoveryFeedbackResponse PlaylistProjectionCatchingUp(DateTimeOffset requestedAt) =>
        this.projectedDiscovery! with
        {
            Status = "scheduled",
            NextEligibleAt = requestedAt.Add(PendingRetryDelay),
            EarliestExpectedCompletionAt = requestedAt.Add(PendingCompletionDelay),
            Reason = "Playlist projection is still catching up.",
            UpdatedAtUtc = requestedAt
        };
}
