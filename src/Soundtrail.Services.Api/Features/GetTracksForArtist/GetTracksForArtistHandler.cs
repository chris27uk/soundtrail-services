using Soundtrail.Adapters.Timing;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Events;
using Soundtrail.Domain.Search;
using Soundtrail.Services.Api.Features.GetTracksForArtist.Adapters;
using Soundtrail.Services.Api.Features.GetTracksForArtist.Contract;

namespace Soundtrail.Services.Api.Features.GetTracksForArtist;

public sealed class GetTracksForArtistHandler(
    IGetTracksForArtistPort getTracksForArtistPort,
    ICommandBus commandBus,
    IClockPort clock) : IApiHandler<GetTracksForArtistRequest, GetTracksForArtistResponse?>
{
    public async Task<GetTracksForArtistResponse?> Handle(GetTracksForArtistRequest request, CancellationToken cancellationToken = default)
    {
        var requestedAt = clock.UtcNow;
        await commandBus.SendAsync(
            new RequestKnownMusicDataCommand(
                new CatalogItemOperation.ChildTracksForArtist(request.ArtistId),
                LookupPriorityBand.High,
                100,
                0,
                requestedAt)
            {
                CreatedAt = requestedAt
            },
            cancellationToken);

        return await getTracksForArtistPort.GetTracksForArtistAsync(request.ArtistId, cancellationToken);
    }
}
