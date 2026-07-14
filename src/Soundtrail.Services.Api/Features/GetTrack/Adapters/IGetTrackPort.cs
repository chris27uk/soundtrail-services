using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Services.Api.Features.GetTrack.Contract;

namespace Soundtrail.Services.Api.Features.GetTrack.Adapters;

public interface IGetTrackPort
{
    Task<GetTrackResponse?> GetTrackAsync(TrackId trackId, CancellationToken cancellationToken);
}
