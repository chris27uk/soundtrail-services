using Soundtrail.Adapters.Timing;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Discovery;
using Soundtrail.Services.Api.Features.Catalog.GetAlbumsForArtist;
using Soundtrail.Services.Api.Features.Catalog.GetAlbumsForArtist.Adapters;
using Soundtrail.Services.Api.Features.Catalog.GetAlbumsForArtist.Contract;

namespace Soundtrail.Services.Tests.Unit.GetAlbumsForArtist;

internal sealed class GetAlbumsForArtistMissingUnitTestEnvironment
{
    private GetAlbumsForArtistMissingUnitTestEnvironment(
        ArtistId artistId,
        GetAlbumsForArtistPortFake port,
        CommandBusFake commandBus,
        ClockPortFake clock)
    {
        ArtistId = artistId;
        Port = port;
        CommandBus = commandBus;
        Clock = clock;
    }

    public ArtistId ArtistId { get; }

    public GetAlbumsForArtistPortFake Port { get; }

    public CommandBusFake CommandBus { get; }

    public ClockPortFake Clock { get; }

    public static GetAlbumsForArtistMissingUnitTestEnvironment ForMissingArtistAlbums(ArtistId? artistId = null) =>
        new(
            artistId ?? ArtistId.From("artist-1707"),
            new GetAlbumsForArtistPortFake(),
            new CommandBusFake(),
            new ClockPortFake(new DateTimeOffset(2024, 6, 7, 8, 9, 10, TimeSpan.Zero)));

    public GetAlbumsForArtistHandler CreateSubjectUnderTest() => new(Port, CommandBus, Clock);

    public GetAlbumsForArtistRequest CreateRequest() => new(ArtistId);

    public sealed class GetAlbumsForArtistPortFake : IGetAlbumsForArtistPort
    {
        public List<ArtistId> RequestedArtistIds { get; } = [];

        public Task<GetAlbumsForArtistResponse?> GetAlbumsForArtistAsync(ArtistId artistId, CancellationToken cancellationToken)
        {
            RequestedArtistIds.Add(artistId);
            return Task.FromResult<GetAlbumsForArtistResponse?>(null);
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
