using Soundtrail.Adapters.Timing;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog.Albums;
using Soundtrail.Domain.Discovery;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForAlbum;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForAlbum.Adapters;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForAlbum.Contract;

namespace Soundtrail.Services.Tests.Unit.GetTracksForAlbum;

internal sealed class GetTracksForAlbumUnitTestEnvironment
{
    private GetTracksForAlbumUnitTestEnvironment(
        AlbumId albumId,
        GetTracksForAlbumPortFake getTracksForAlbumPortFake,
        CommandBusFake commandBus,
        ClockPortFake clock)
    {
        AlbumId = albumId;
        Port = getTracksForAlbumPortFake;
        CommandBus = commandBus;
        Clock = clock;
    }

    public AlbumId AlbumId { get; }

    public GetTracksForAlbumPortFake Port { get; }

    public CommandBusFake CommandBus { get; }

    public ClockPortFake Clock { get; }

    public static GetTracksForAlbumUnitTestEnvironment ForExistingAlbumTracks(
        AlbumId? albumId = null,
        GetTracksForAlbumResponse? response = null) =>
        new(
            albumId ?? AlbumTracks.DefaultAlbumId,
            new GetTracksForAlbumPortFake(response ?? AlbumTracks.CreateResponse(albumId: albumId ?? AlbumTracks.DefaultAlbumId)),
            new CommandBusFake(),
            new ClockPortFake(new DateTimeOffset(2024, 6, 7, 8, 9, 10, TimeSpan.Zero)));

    public GetTracksForAlbumHandler CreateSubjectUnderTest() => new(Port, CommandBus, Clock);

    public GetTracksForAlbumRequest CreateRequest() => new(AlbumId);

    public sealed class GetTracksForAlbumPortFake(GetTracksForAlbumResponse? response) : IGetTracksForAlbumPort
    {
        public List<AlbumId> RequestedAlbumIds { get; } = [];

        public Task<GetTracksForAlbumResponse?> GetTracksForAlbumAsync(AlbumId albumId, CancellationToken cancellationToken)
        {
            RequestedAlbumIds.Add(albumId);
            return Task.FromResult(response);
        }
    }

    public sealed class CommandBusFake : ICommandBus
    {
        public List<RequestKnownMusicDataMessage> Commands { get; } = [];

        public Task SendAsync(IMessage message, CancellationToken cancellationToken = default)
        {
            Commands.Add((RequestKnownMusicDataMessage)message);
            return Task.CompletedTask;
        }
    }

    public sealed class ClockPortFake(DateTimeOffset utcNow) : IClockPort
    {
        public DateTimeOffset UtcNow { get; } = utcNow;
    }
}
