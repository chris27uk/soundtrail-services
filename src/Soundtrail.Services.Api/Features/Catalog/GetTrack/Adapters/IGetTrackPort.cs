using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Services.Api.Features.Catalog.GetTrack.Contract;

namespace Soundtrail.Services.Api.Features.Catalog.GetTrack.Adapters;

public interface IGetTrackPort
{
    Task<GetTrackResponse?> GetTrackAsync(TrackId trackId, CancellationToken cancellationToken);
}
