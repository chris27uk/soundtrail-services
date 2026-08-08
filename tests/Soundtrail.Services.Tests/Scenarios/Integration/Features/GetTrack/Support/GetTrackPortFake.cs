using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Services.Api.Features.Catalog.GetTrack.Adapters;
using Soundtrail.Services.Api.Features.Catalog.GetTrack.Contract;

namespace Soundtrail.Services.Tests.Integration.Features.GetTrack.Support;

internal sealed class GetTrackPortFake : IGetTrackPort
{
    private GetTrackResponse? response;

    public GetTrackPortFake(GetTrackResponse? response = null) => this.response = response;

    public List<TrackId> RequestedTrackIds { get; } = [];

    public void Seed(GetTrackResponse? track) => this.response = track;

    public Task<GetTrackResponse?> GetTrackAsync(TrackId trackId, CancellationToken cancellationToken)
    {
        RequestedTrackIds.Add(trackId);
        return Task.FromResult(this.response);
    }
}
