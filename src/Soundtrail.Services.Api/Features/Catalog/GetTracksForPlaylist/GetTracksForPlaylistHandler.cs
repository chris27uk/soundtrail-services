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
            return response with { Discovery = discovery };
        }

        return new GetTracksForPlaylistResponse(
            request.PlaylistId,
            [],
            discovery ?? BuildPendingDiscovery(requestedAt));
    }

    private static DiscoveryFeedbackResponse BuildPendingDiscovery(DateTimeOffset requestedAt) =>
        new(
            "scheduled",
            LookupPriorityBand.High,
            requestedAt.Add(PendingRetryDelay),
            requestedAt.Add(PendingCompletionDelay),
            "Playlist lookup queued.",
            requestedAt);
}
