using Soundtrail.Adapters.Timing;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Catalog.Playlists;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Messages;
using Soundtrail.Services.Enrichment.Worker.Features.LookupPlaylistTracks;
using Soundtrail.Services.Enrichment.Worker.Features.LookupPlaylistTracks.Ports;
using Soundtrail.Services.Enrichment.Worker.Infrastructure.Idempotency.Storage;
using Soundtrail.Services.Enrichment.Worker.Shared.Execution;
using Soundtrail.Services.Enrichment.Worker.Shared.ExecutionAdmission;

namespace Soundtrail.Services.Tests.Unit.Worker.LookupPlaylistTracks;

internal sealed class LookupPlaylistTracksUnitTestEnvironment
{
    private LookupPlaylistTracksUnitTestEnvironment()
    {
        Clock = new ClockFake();
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

    public sealed class ClockFake : IClockPort
    {
        public DateTimeOffset UtcNow { get; set; } = new(2026, 7, 20, 11, 45, 0, TimeSpan.Zero);
    }

    public sealed class CommandBusFake : ICommandBus
    {
        public List<object> Messages { get; } = [];

        public Task SendAsync(IMessage message, CancellationToken cancellationToken = default)
        {
            Messages.Add(message);
            return Task.CompletedTask;
        }

        public Task SendAsync(object message, CancellationToken cancellationToken = default)
        {
            Messages.Add(message);
            return Task.CompletedTask;
        }
    }

    public sealed class ReadPlaylistTracksByProviderPortFake : IReadPlaylistTracksByProviderPort
    {
        public IReadOnlyList<TrackReference> Result { get; set; } = [];

        public Exception? ExceptionToThrow { get; set; }

        public PlaylistId? RequestedPlaylistId { get; private set; }

        public ProviderName? RequestedProvider { get; private set; }

        public Task<IReadOnlyList<TrackReference>> ReadAsync(
            PlaylistId playlistId,
            ProviderName provider,
            CancellationToken cancellationToken)
        {
            RequestedPlaylistId = playlistId;
            RequestedProvider = provider;

            if (ExceptionToThrow is not null)
            {
                throw ExceptionToThrow;
            }

            return Task.FromResult(Result);
        }
    }

    public sealed class LookupExecutionAdmissionPortFake : ILookupExecutionAdmissionPort
    {
        public LookupExecutionAdmissionResult Result { get; set; } = LookupExecutionAdmissionResult.Acquired();

        public LookupExecutionAdmissionRequest? RequestedAdmission { get; private set; }

        public List<MessageId> CommittedCommandIds { get; } = [];

        public List<MessageId> ReleasedCommandIds { get; } = [];

        public Task<LookupExecutionAdmissionResult> TryAcquireAsync(
            LookupExecutionAdmissionRequest request,
            CancellationToken cancellationToken)
        {
            RequestedAdmission = request;
            return Task.FromResult(Result);
        }

        public Task CommitAsync(MessageId messageId, CancellationToken cancellationToken)
        {
            CommittedCommandIds.Add(messageId);
            return Task.CompletedTask;
        }

        public Task ReleaseAsync(MessageId messageId, CancellationToken cancellationToken)
        {
            ReleasedCommandIds.Add(messageId);
            return Task.CompletedTask;
        }
    }

    public sealed class LookupExecutionReceiptStoreFake : ILookupExecutionReceiptStore
    {
        public bool TryBeginResult { get; set; } = true;

        public List<MessageId> TryBeginCommandIds { get; } = [];

        public List<MessageId> CompletedCommandIds { get; } = [];

        public List<MessageId> ReleasedCommandIds { get; } = [];

        public Task<bool> TryBeginAsync(MessageId messageId, CancellationToken cancellationToken)
        {
            TryBeginCommandIds.Add(messageId);
            return Task.FromResult(TryBeginResult);
        }

        public Task MarkCompletedAsync(MessageId messageId, CancellationToken cancellationToken)
        {
            CompletedCommandIds.Add(messageId);
            return Task.CompletedTask;
        }

        public Task ReleaseAsync(MessageId messageId, CancellationToken cancellationToken)
        {
            ReleasedCommandIds.Add(messageId);
            return Task.CompletedTask;
        }
    }

    public sealed class InnerHandlerFake : IHandler<LookupPlaylistTracksByProviderMessage>
    {
        public int Calls { get; private set; }

        public Exception? ExceptionToThrow { get; set; }

        public LookupPlaylistTracksByProviderMessage? Request { get; private set; }

        public Task Handle(LookupPlaylistTracksByProviderMessage request, CancellationToken cancellationToken = default)
        {
            Calls++;
            Request = request;

            if (ExceptionToThrow is not null)
            {
                throw ExceptionToThrow;
            }

            return Task.CompletedTask;
        }
    }
}
