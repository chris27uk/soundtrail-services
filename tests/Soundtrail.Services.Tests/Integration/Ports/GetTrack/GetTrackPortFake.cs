using Soundtrail.Domain.Catalog;
using Soundtrail.Services.Api.Features.GetTrack.Adapters;
using Soundtrail.Services.Api.Features.GetTrack.Contract;

namespace Soundtrail.Services.Tests.Integration.Ports.GetTrack;

internal sealed class GetTrackPortFake(GetTrackResponse? response = null) : IGetTrackPort
{
    public Task<GetTrackResponse?> GetTrackAsync(TrackId trackId, CancellationToken cancellationToken) => Task.FromResult(response);
}
