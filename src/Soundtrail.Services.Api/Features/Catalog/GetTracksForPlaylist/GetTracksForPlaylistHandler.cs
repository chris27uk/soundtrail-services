using Soundtrail.Adapters.Timing;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Services.Api.Features.Catalog.Shared.Contract;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForPlaylist.Adapters;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForPlaylist.Contract;
using Soundtrail.Services.Api.Features.Catalog.Search.Adapters;

namespace Soundtrail.Services.Api.Features.Catalog.GetTracksForPlaylist;

public sealed class GetTracksForPlaylistHandler(
    IGetTracksForPlaylistPort getTracksForPlaylistPort,
    ICommandBus commandBus,
    IDiscoveryFeedbackPort discoveryFeedbackPort,
    IClockPort clock) : IApiHandler<GetTracksForPlaylistRequest, GetTracksForPlaylistResponse?>
{
    private static readonly TimeSpan PendingRetryDelay = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan PendingCompletionDelay = TimeSpan.FromSeconds(75);

    public async Task<GetTracksForPlaylistResponse?> Handle(GetTracksForPlaylistRequest request, CancellationToken cancellationToken = default)
    {
        var requestedAt = clock.UtcNow;
        await commandBus.SendAsync(
            new RequestKnownMusicDataMessage(
                new CatalogItemOperation.ChildTracksForPlaylist(request.PlaylistId),
                LookupPriorityBand.High,
                100,
                0,
                requestedAt)
            {
                CreatedAt = requestedAt
            },
            cancellationToken);

        var response = await getTracksForPlaylistPort.GetTracksForPlaylistAsync(request.PlaylistId, cancellationToken);
        var discovery = await discoveryFeedbackPort.GetAsync(
            new EnrichmentTarget.KnownCatalogItemOperation(new CatalogItemOperation.ChildTracksForPlaylist(request.PlaylistId)),
            cancellationToken);

        if (response is not null)
        {
            var combinedDiscovery = await BuildDiscoveryAsync(response, discovery, cancellationToken);
            return response with { Discovery = combinedDiscovery };
        }

        return new GetTracksForPlaylistResponse(
            request.PlaylistId,
            [],
            BuildMissingProjectionDiscovery(discovery, requestedAt));
    }

    private static DiscoveryFeedbackResponse BuildPendingDiscovery(DateTimeOffset requestedAt) =>
        new(
            "scheduled",
            LookupPriorityBand.High,
            requestedAt.Add(PendingRetryDelay),
            requestedAt.Add(PendingCompletionDelay),
            "Playlist lookup queued.",
            requestedAt);

    private async Task<DiscoveryFeedbackResponse?> BuildDiscoveryAsync(
        GetTracksForPlaylistResponse response,
        DiscoveryFeedbackResponse? playlistDiscovery,
        CancellationToken cancellationToken)
    {
        if (playlistDiscovery is null)
        {
            return null;
        }

        if (response.Tracks.Length == 0 && playlistDiscovery.Status == "completed")
        {
            return BuildPlaylistProjectionPendingDiscovery(playlistDiscovery, clock.UtcNow);
        }

        var trackDiscoveries = new List<DiscoveryFeedbackResponse>();

        foreach (var track in response.Tracks)
        {
            var trackDiscovery = await discoveryFeedbackPort.GetAsync(
                new EnrichmentTarget.KnownCatalogItemOperation(new CatalogItemOperation.StreamingLocationForTrack(track.TrackId)),
                cancellationToken);

            if (trackDiscovery is not null)
            {
                trackDiscoveries.Add(trackDiscovery);
            }

            if (!track.Playable && IsIncomplete(trackDiscovery))
            {
                return BuildStreamingPendingDiscovery(trackDiscovery, playlistDiscovery, clock.UtcNow);
            }

            if (!track.Playable && trackDiscovery?.Status == "completed")
            {
                return BuildStreamingProjectionPendingDiscovery(trackDiscovery, clock.UtcNow);
            }
        }

        if (IsIncomplete(playlistDiscovery))
        {
            return playlistDiscovery;
        }

        return MostRecentCompletedOrOriginal(trackDiscoveries, playlistDiscovery);
    }

    private static bool IsIncomplete(DiscoveryFeedbackResponse? discovery) =>
        discovery is null
        || discovery.Status is "requested" or "scheduled" or "deferred";

    private static DiscoveryFeedbackResponse BuildMissingProjectionDiscovery(
        DiscoveryFeedbackResponse? discovery,
        DateTimeOffset requestedAt)
    {
        if (discovery?.Status != "completed")
        {
            return discovery ?? BuildPendingDiscovery(requestedAt);
        }

        return BuildPlaylistProjectionPendingDiscovery(discovery, requestedAt);
    }

    private static DiscoveryFeedbackResponse BuildStreamingPendingDiscovery(
        DiscoveryFeedbackResponse? trackDiscovery,
        DiscoveryFeedbackResponse? playlistDiscovery,
        DateTimeOffset requestedAt)
    {
        if (trackDiscovery is not null)
        {
            return trackDiscovery;
        }

        var basis = playlistDiscovery ?? BuildPendingDiscovery(requestedAt);
        return basis with
        {
            Status = "scheduled",
            NextEligibleAt = requestedAt.Add(PendingRetryDelay),
            EarliestExpectedCompletionAt = requestedAt.Add(PendingCompletionDelay),
            Reason = "Track streaming projection is still catching up.",
            UpdatedAtUtc = requestedAt
        };
    }

    private static DiscoveryFeedbackResponse BuildPlaylistProjectionPendingDiscovery(
        DiscoveryFeedbackResponse discovery,
        DateTimeOffset requestedAt) =>
        discovery with
        {
            Status = "scheduled",
            NextEligibleAt = requestedAt.Add(PendingRetryDelay),
            EarliestExpectedCompletionAt = requestedAt.Add(PendingCompletionDelay),
            Reason = "Playlist projection is still catching up.",
            UpdatedAtUtc = requestedAt
        };

    private static DiscoveryFeedbackResponse BuildStreamingProjectionPendingDiscovery(
        DiscoveryFeedbackResponse discovery,
        DateTimeOffset requestedAt) =>
        discovery with
        {
            Status = "scheduled",
            NextEligibleAt = requestedAt.Add(PendingRetryDelay),
            EarliestExpectedCompletionAt = requestedAt.Add(PendingCompletionDelay),
            Reason = "Track streaming projection is still catching up.",
            UpdatedAtUtc = requestedAt
        };

    private static DiscoveryFeedbackResponse? MostRecentCompletedOrOriginal(
        IReadOnlyCollection<DiscoveryFeedbackResponse> trackDiscoveries,
        DiscoveryFeedbackResponse? playlistDiscovery)
    {
        var completed = trackDiscoveries
            .Where(static discovery => discovery.Status == "completed")
            .OrderByDescending(static discovery => discovery.UpdatedAtUtc)
            .FirstOrDefault();

        if (completed is not null && playlistDiscovery?.Status == "completed")
        {
            return playlistDiscovery with
            {
                UpdatedAtUtc = completed.UpdatedAtUtc
            };
        }

        return playlistDiscovery;
    }
}
