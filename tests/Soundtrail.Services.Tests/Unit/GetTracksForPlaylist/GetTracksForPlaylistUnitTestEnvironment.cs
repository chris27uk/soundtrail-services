using Soundtrail.Adapters.Timing;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog.Playlists;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForPlaylist;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForPlaylist.Adapters;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForPlaylist.Contract;
using Soundtrail.Services.Tests.Fakes;

namespace Soundtrail.Services.Tests.Unit.GetTracksForPlaylist;

internal sealed class GetTracksForPlaylistUnitTestEnvironment
{
    private GetTracksForPlaylistUnitTestEnvironment(
        PlaylistId playlistId,
        GetTracksForPlaylistPortFake port,
        CommandBusFake commandBus,
        ClockFake clock)
    {
        PlaylistId = playlistId;
        Port = port;
        CommandBus = commandBus;
        Clock = clock;
    }

    public PlaylistId PlaylistId { get; }

    public GetTracksForPlaylistPortFake Port { get; }

    public CommandBusFake CommandBus { get; }

    public ClockFake Clock { get; }

    public static GetTracksForPlaylistUnitTestEnvironment ForExistingPlaylistTracks(
        PlaylistId? playlistId = null,
        GetTracksForPlaylistResponse? response = null) =>
        new(
            playlistId ?? PlaylistTracks.DefaultPlaylistId,
            GetTracksForPlaylistPortFake.Create().WithPlaylistTracks(
                response ?? PlaylistTracks.CreateResponse(playlistId: playlistId ?? PlaylistTracks.DefaultPlaylistId)),
            new CommandBusFake(),
            new ClockFake(new DateTimeOffset(2024, 6, 7, 8, 9, 10, TimeSpan.Zero)));

    public static GetTracksForPlaylistUnitTestEnvironment ForMissingPlaylistTracks(PlaylistId? playlistId = null) =>
        new(
            playlistId ?? PlaylistId.FromPlaylistName("WorldwideSongChart"),
            GetTracksForPlaylistPortFake.Create(),
            new CommandBusFake(),
            new ClockFake(new DateTimeOffset(2024, 6, 7, 8, 9, 10, TimeSpan.Zero)));

    public GetTracksForPlaylistHandler CreateSubjectUnderTest() => new(Port, CommandBus, Clock);

    public GetTracksForPlaylistRequest CreateRequest() => new(PlaylistId);

    public TMessage SentMessage<TMessage>() where TMessage : IMessage =>
        CommandBus.SentMessages.OfType<TMessage>().Single();
}
