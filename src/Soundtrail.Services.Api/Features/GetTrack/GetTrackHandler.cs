using Soundtrail.Domain.Abstractions;
using Soundtrail.Services.Api.Features.GetTrack.Adapters;
using Soundtrail.Services.Api.Features.GetTrack.Contract;

namespace Soundtrail.Services.Api.Features.GetTrack;

public sealed class GetTrackHandler(IGetTrackPort getTrackPort) : IApiHandler<GetTrackRequest, GetTrackResponse?>
{
    public async Task<GetTrackResponse?> Handle(GetTrackRequest request, CancellationToken cancellationToken = default) =>
        await getTrackPort.GetTrackAsync(request.TrackId, cancellationToken);
}
