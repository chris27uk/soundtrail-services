using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Services.Api.Features.Catalog.GetTrack;
using Soundtrail.Services.Api.Features.Catalog.GetTrack.Adapters;
using Soundtrail.Services.Api.Features.Catalog.GetTrack.Contract;

namespace Soundtrail.Services.Tests.Unit.GetTrack;

internal sealed class GetTrackMissingUnitTestEnvironment
{
    private GetTrackMissingUnitTestEnvironment(
        TrackId trackId,
        GetTrackPortFake getTrackPortFake)
    {
        TrackId = trackId;
        Port = getTrackPortFake;
    }

    public TrackId TrackId { get; }

    public GetTrackPortFake Port { get; }

    public static GetTrackMissingUnitTestEnvironment ForMissingTrack(TrackId? trackId = null) =>
        new(
            trackId ?? TestTrackIds.Create("track-401"),
            new GetTrackPortFake());

    public GetTrackHandler CreateSubjectUnderTest() => new(Port);

    public GetTrackRequest CreateRequest() => new(TrackId);

    public sealed class GetTrackPortFake : IGetTrackPort
    {
        public List<TrackId> RequestedTrackIds { get; } = [];

        public Task<GetTrackResponse?> GetTrackAsync(TrackId trackId, CancellationToken cancellationToken)
        {
            RequestedTrackIds.Add(trackId);
            return Task.FromResult<GetTrackResponse?>(null);
        }
    }
}
