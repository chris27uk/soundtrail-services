using Soundtrail.Domain.Abstractions;
using Soundtrail.Services.Api.Features.Catalog.GetTrack.Adapters;
using Soundtrail.Services.Api.Features.Catalog.GetTrack.Contract;

namespace Soundtrail.Services.Api.Features.Catalog.GetTrack;

public sealed class GetTrackHandler(IGetTrackPort getTrackPort) : IApiHandler<GetTrackRequest, GetTrackResponse?>
{
    public async Task<GetTrackResponse?> Handle(GetTrackRequest request, CancellationToken cancellationToken = default) =>
        await getTrackPort.GetTrackAsync(request.TrackId, cancellationToken);
}
