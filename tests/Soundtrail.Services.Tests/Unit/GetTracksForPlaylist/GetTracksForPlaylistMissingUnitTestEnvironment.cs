using Soundtrail.Adapters.Timing;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog.Playlists;
using Soundtrail.Domain.Discovery;
using Soundtrail.Services.Api.Features.GetTracksForPlaylist;
using Soundtrail.Services.Api.Features.GetTracksForPlaylist.Adapters;
using Soundtrail.Services.Api.Features.GetTracksForPlaylist.Contract;

namespace Soundtrail.Services.Tests.Unit.GetTracksForPlaylist;

internal sealed class GetTracksForPlaylistMissingUnitTestEnvironment
{
    private GetTracksForPlaylistMissingUnitTestEnvironment(
        PlaylistId playlistId,
        GetTracksForPlaylistPortFake port,
        CommandBusFake commandBus,
        ClockPortFake clock)
    {
        PlaylistId = playlistId;
        Port = port;
        CommandBus = commandBus;
        Clock = clock;
    }

    public PlaylistId PlaylistId { get; }

    public GetTracksForPlaylistPortFake Port { get; }

    public CommandBusFake CommandBus { get; }

    public ClockPortFake Clock { get; }

    public static GetTracksForPlaylistMissingUnitTestEnvironment ForMissingPlaylistTracks(PlaylistId? playlistId = null) =>
        new(
            playlistId ?? PlaylistId.FromPlaylistName("WorldwideSongChart"),
            new GetTracksForPlaylistPortFake(),
            new CommandBusFake(),
            new ClockPortFake(new DateTimeOffset(2024, 6, 7, 8, 9, 10, TimeSpan.Zero)));

    public GetTracksForPlaylistHandler CreateSubjectUnderTest() => new(Port, CommandBus, Clock);

    public GetTracksForPlaylistRequest CreateRequest() => new(PlaylistId);

    public sealed class GetTracksForPlaylistPortFake : IGetTracksForPlaylistPort
    {
        public List<PlaylistId> RequestedPlaylistIds { get; } = [];

        public Task<GetTracksForPlaylistResponse?> GetTracksForPlaylistAsync(PlaylistId playlistId, CancellationToken cancellationToken)
        {
            RequestedPlaylistIds.Add(playlistId);
            return Task.FromResult<GetTracksForPlaylistResponse?>(null);
        }
    }

    public sealed class CommandBusFake : ICommandBus
    {
        public List<ICommand> Commands { get; } = [];

        public Task SendAsync(ICommand command, CancellationToken cancellationToken = default)
        {
            Commands.Add(command);
            return Task.CompletedTask;
        }
    }

    public sealed class ClockPortFake(DateTimeOffset utcNow) : IClockPort
    {
        public DateTimeOffset UtcNow { get; } = utcNow;
    }
}
