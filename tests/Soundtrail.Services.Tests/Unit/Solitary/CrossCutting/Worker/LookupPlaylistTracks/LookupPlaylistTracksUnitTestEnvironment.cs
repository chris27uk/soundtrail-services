using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Catalog.Playlists;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery.Messages;
using Soundtrail.Services.Enrichment.Worker.Features.LookupPlaylistTracks;
using Soundtrail.Services.Enrichment.Worker.Shared.Execution;
using Soundtrail.Services.Tests.Unit.Sociable.Features.GetTracksForPlaylist;
using Soundtrail.Services.Tests.Unit.Sociable.Infrastructure.Fakes;
using Soundtrail.Services.Tests.Unit.Sociable.Features.GetTracksForPlaylist.Support;

namespace Soundtrail.Services.Tests.Unit.Solitary.CrossCutting.Worker.LookupPlaylistTracks;

internal sealed class LookupPlaylistTracksUnitTestEnvironment
{
    private LookupPlaylistTracksUnitTestEnvironment()
    {
        Clock = new ClockFake(new DateTimeOffset(2026, 7, 20, 11, 45, 0, TimeSpan.Zero));
        CommandBus = new CommandBusFake();
        ReadPlaylistTracksByProviderPort = new ReadPlaylistTracksByProviderPortFake();
        AdmissionPort = new LookupExecutionAdmissionPortFake();
        ReceiptStore = new LookupExecutionReceiptStoreFake();
        InnerHandler = new InnerHandlerFake();
    }

    public ClockFake Clock { get; }

    public CommandBusFake CommandBus { get; }

    public ReadPlaylistTracksByProviderPortFake ReadPlaylistTracksByProviderPort { get; }

    public LookupExecutionAdmissionPortFake AdmissionPort { get; }

    public LookupExecutionReceiptStoreFake ReceiptStore { get; }

    public InnerHandlerFake InnerHandler { get; }

    public static LookupPlaylistTracksUnitTestEnvironment Create() => new();

    public LookupPlaylistTracksByProviderHandler CreateBusinessSubject() =>
        new(ReadPlaylistTracksByProviderPort, Clock, CommandBus);

    public AdmittedLookupHandlerDecorator<LookupPlaylistTracksByProviderMessage> CreateAdmissionSubject(
        IHandler<LookupPlaylistTracksByProviderMessage>? inner = null) =>
        new(inner ?? InnerHandler, new LookupPlaylistTracksByProviderDecoratorMetadata(), CommandBus, AdmissionPort, Clock);

    public IdempotentLookupHandlerDecorator<LookupPlaylistTracksByProviderMessage> CreateIdempotencySubject(
        IHandler<LookupPlaylistTracksByProviderMessage>? inner = null) =>
        new(inner ?? InnerHandler, new LookupPlaylistTracksByProviderDecoratorMetadata(), ReceiptStore, CommandBus, Clock);

    public LookupPlaylistTracksByProviderMessage CreateRequest(
        string playlistName = "WorldwideSongChart",
        string commandId = "cmd-playlist-lookup",
        DateTimeOffset? createdAt = null) =>
        new(
            MessageId.For(commandId),
            CorrelationId.From($"corr:{commandId}"),
            createdAt ?? new DateTimeOffset(2026, 7, 20, 10, 30, 0, TimeSpan.Zero),
            LookupPriorityBand.High,
            PlaylistId.FromPlaylistName(playlistName),
            ProviderName.Spotify);

    public static IReadOnlyList<TrackReference> CreateTrackReferences(params (string ArtistName, string TrackTitle)[] values) =>
        values.Select(static value => new TrackReference(ArtistName.From(value.ArtistName), value.TrackTitle)).ToArray();

    public sealed class InnerHandlerFake : IHandler<LookupPlaylistTracksByProviderMessage>
    {
        public int Calls { get; private set; }

        public Exception? ExceptionToThrow { get; set; }

        public LookupPlaylistTracksByProviderMessage? Request { get; private set; }

        public Task Handle(IncomingMessage<LookupPlaylistTracksByProviderMessage> context, CancellationToken cancellationToken = default)
        {
            Calls++;
            Request = context.Message;

            if (ExceptionToThrow is not null)
            {
                throw ExceptionToThrow;
            }

            return Task.CompletedTask;
        }
    }
}
