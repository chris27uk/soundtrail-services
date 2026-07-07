using Soundtrail.Domain.Catalog;
using Soundtrail.Services.Api.Features.GetTrack;
using Soundtrail.Services.Api.Features.GetTrack.Adapters;
using Soundtrail.Services.Api.Features.GetTrack.Contract;

namespace Soundtrail.Services.Tests.Unit.GetTrack;

internal sealed class GetTrackUnitTestEnvironment
{
    private GetTrackUnitTestEnvironment(
        TrackId trackId,
        GetTrackPortFake getTrackPortFake)
    {
        TrackId = trackId;
        Port = getTrackPortFake;
    }

    public TrackId TrackId { get; }

    public GetTrackPortFake Port { get; }

    public static GetTrackUnitTestEnvironment ForExistingTrack(
        TrackId? trackId = null,
        GetTrackResponse? response = null) =>
        new(
            trackId ?? Tracks.DefaultTrackId,
            new GetTrackPortFake(response ?? Tracks.CreateTrackResponse(trackId: trackId ?? Tracks.DefaultTrackId)));

    public GetTrackHandler CreateSubjectUnderTest() => new(Port);

    public GetTrackRequest CreateRequest() => new(TrackId);

    public sealed class GetTrackPortFake(GetTrackResponse? response) : IGetTrackPort
    {
        public List<TrackId> RequestedTrackIds { get; } = [];

        public Task<GetTrackResponse?> GetTrackAsync(TrackId trackId, CancellationToken cancellationToken)
        {
            RequestedTrackIds.Add(trackId);
            return Task.FromResult(response);
        }
    }
}
