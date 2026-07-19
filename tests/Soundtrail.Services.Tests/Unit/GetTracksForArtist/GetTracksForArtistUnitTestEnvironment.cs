using Soundtrail.Adapters.Timing;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Discovery;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForArtist;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForArtist.Adapters;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForArtist.Contract;

namespace Soundtrail.Services.Tests.Unit.GetTracksForArtist;

internal sealed class GetTracksForArtistUnitTestEnvironment
{
    private GetTracksForArtistUnitTestEnvironment(
        ArtistId artistId,
        GetTracksForArtistPortFake port,
        CommandBusFake commandBus,
        ClockPortFake clock)
    {
        ArtistId = artistId;
        Port = port;
        CommandBus = commandBus;
        Clock = clock;
    }

    public ArtistId ArtistId { get; }

    public GetTracksForArtistPortFake Port { get; }

    public CommandBusFake CommandBus { get; }

    public ClockPortFake Clock { get; }

    public static GetTracksForArtistUnitTestEnvironment ForExistingArtistTracks(
        ArtistId? artistId = null,
        GetTracksForArtistResponse? response = null) =>
        new(
            artistId ?? ArtistTracks.DefaultArtistId,
            new GetTracksForArtistPortFake(response ?? ArtistTracks.CreateResponse(artistId: artistId ?? ArtistTracks.DefaultArtistId)),
            new CommandBusFake(),
            new ClockPortFake(new DateTimeOffset(2024, 6, 7, 8, 9, 10, TimeSpan.Zero)));

    public GetTracksForArtistHandler CreateSubjectUnderTest() => new(Port, CommandBus, Clock);

    public GetTracksForArtistRequest CreateRequest() => new(ArtistId);

    public sealed class GetTracksForArtistPortFake(GetTracksForArtistResponse? response) : IGetTracksForArtistPort
    {
        public List<ArtistId> RequestedArtistIds { get; } = [];

        public Task<GetTracksForArtistResponse?> GetTracksForArtistAsync(ArtistId artistId, CancellationToken cancellationToken)
        {
            RequestedArtistIds.Add(artistId);
            return Task.FromResult(response);
        }
    }

    public sealed class CommandBusFake : ICommandBus
    {
        public List<RequestKnownMusicDataCommand> Commands { get; } = [];

        public Task SendAsync(ICommand command, CancellationToken cancellationToken = default)
        {
            Commands.Add((RequestKnownMusicDataCommand)command);
            return Task.CompletedTask;
        }
    }

    public sealed class ClockPortFake(DateTimeOffset utcNow) : IClockPort
    {
        public DateTimeOffset UtcNow { get; } = utcNow;
    }
}
