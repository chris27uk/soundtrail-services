using Soundtrail.Adapters.Timing;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery.Messages;
using Soundtrail.Services.Enrichment.Worker.Features.LookupStreamingLocationByIsrc;
using Soundtrail.Services.Enrichment.Worker.Features.LookupStreamingLocationByTrackMetadata;
using Soundtrail.Services.Enrichment.Worker.Infrastructure.Idempotency.Storage;
using Soundtrail.Services.Enrichment.Worker.Shared.ExecutionAdmission;
using Soundtrail.Services.Enrichment.Worker.Shared.StreamingLocations;

namespace Soundtrail.Services.Tests.Unit.Worker.LookupStreamingLocations;

internal sealed class LookupStreamingLocationsUnitTestEnvironment
{
    private LookupStreamingLocationsUnitTestEnvironment()
    {
        Clock = new ClockFake();
        CommandBus = new CommandBusFake();
        ReadTrackForLookupPort = new ReadTrackForLookupPortFake();
        ReadStreamingLocationByProviderPort = new ReadStreamingLocationByProviderPortFake();
        AdmissionPort = new LookupExecutionAdmissionPortFake();
        ReceiptStore = new LookupExecutionReceiptStoreFake();
        IsrcInnerHandler = new IsrcInnerHandlerFake();
        MetadataInnerHandler = new MetadataInnerHandlerFake();
    }

    public ClockFake Clock { get; }

    public CommandBusFake CommandBus { get; }

    public ReadTrackForLookupPortFake ReadTrackForLookupPort { get; }

    public ReadStreamingLocationByProviderPortFake ReadStreamingLocationByProviderPort { get; }

    public LookupExecutionAdmissionPortFake AdmissionPort { get; }

    public LookupExecutionReceiptStoreFake ReceiptStore { get; }

    public IsrcInnerHandlerFake IsrcInnerHandler { get; }

    public MetadataInnerHandlerFake MetadataInnerHandler { get; }

    public static LookupStreamingLocationsUnitTestEnvironment Create() => new();

    public LookupStreamingLocationByIsrcHandler CreateIsrcBusinessSubject() =>
        new(ReadTrackForLookupPort, ReadStreamingLocationByProviderPort, Clock, CommandBus);

    public LookupStreamingLocationByTrackMetadataHandler CreateMetadataBusinessSubject() =>
        new(ReadTrackForLookupPort, ReadStreamingLocationByProviderPort, Clock, CommandBus);

    public AdmittedLookupStreamingLocationByIsrcHandlerDecorator CreateIsrcAdmissionSubject(
        IHandler<LookupStreamingLocationByIsrcMessage>? inner = null) =>
        new(inner ?? IsrcInnerHandler, CommandBus, AdmissionPort, Clock);

    public AdmittedLookupStreamingLocationByTrackMetadataHandlerDecorator CreateMetadataAdmissionSubject(
        IHandler<LookupStreamingLocationByTrackMetadataMessage>? inner = null) =>
        new(inner ?? MetadataInnerHandler, CommandBus, AdmissionPort, Clock);

    public IdempotentLookupStreamingLocationByIsrcHandlerDecorator CreateIsrcIdempotencySubject(
        IHandler<LookupStreamingLocationByIsrcMessage>? inner = null) =>
        new(inner ?? IsrcInnerHandler, ReceiptStore, CommandBus, Clock);

    public IdempotentLookupStreamingLocationByTrackMetadataHandlerDecorator CreateMetadataIdempotencySubject(
        IHandler<LookupStreamingLocationByTrackMetadataMessage>? inner = null) =>
        new(inner ?? MetadataInnerHandler, ReceiptStore, CommandBus, Clock);

    public LookupStreamingLocationByIsrcMessage CreateIsrcRequest(
        string commandId = "cmd-streaming-isrc",
        string? trackId = null,
        DateTimeOffset? createdAt = null) =>
        new(
            MessageId.For(commandId),
            CorrelationId.From($"corr:{commandId}"),
            createdAt ?? new DateTimeOffset(2026, 7, 20, 10, 30, 0, TimeSpan.Zero),
            LookupPriorityBand.High,
            TrackId.From(trackId ?? global::Soundtrail.Services.Tests.TestTrackIds.Value("streaming-track-01")),
            ProviderName.Spotify);

    public LookupStreamingLocationByTrackMetadataMessage CreateMetadataRequest(
        string commandId = "cmd-streaming-metadata",
        string? trackId = null,
        DateTimeOffset? createdAt = null) =>
        new(
            MessageId.For(commandId),
            CorrelationId.From($"corr:{commandId}"),
            createdAt ?? new DateTimeOffset(2026, 7, 20, 10, 30, 0, TimeSpan.Zero),
            LookupPriorityBand.High,
            TrackId.From(trackId ?? global::Soundtrail.Services.Tests.TestTrackIds.Value("streaming-track-02")),
            ProviderName.AppleMusic);

    public static TrackLookupContext CreateTrack(
        string seed = "streaming-track",
        string artistId = "artist-lookup-01",
        string title = "Road Song",
        string artistName = "The Travellers",
        string? isrc = "GBAYE2409901") =>
        new(
            Soundtrail.Domain.Catalog.Artists.ArtistId.From(artistId),
            global::Soundtrail.Services.Tests.TestTrackIds.Create(seed),
            title,
            artistName,
            isrc);

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

    public sealed class ReadTrackForLookupPortFake : IReadTrackForLookupPort
    {
        public TrackLookupContext? Result { get; set; } = CreateTrack();

        public TrackId? RequestedTrackId { get; private set; }

        public Task<TrackLookupContext?> ReadAsync(TrackId trackId, CancellationToken cancellationToken)
        {
            RequestedTrackId = trackId;
            return Task.FromResult(Result);
        }
    }

    public sealed class ReadStreamingLocationByProviderPortFake : IReadStreamingLocationByProviderPort
    {
        public Uri? IsrcResult { get; set; } = new("https://open.spotify.com/track/123", UriKind.Absolute);

        public Uri? MetadataResult { get; set; } = new("https://music.apple.com/track/abc", UriKind.Absolute);

        public string? RequestedIsrc { get; private set; }

        public string? RequestedArtistName { get; private set; }

        public string? RequestedTrackTitle { get; private set; }

        public ProviderName? RequestedProvider { get; private set; }

        public Task<Uri?> ReadByIsrcAsync(string isrc, ProviderName provider, CancellationToken cancellationToken)
        {
            RequestedIsrc = isrc;
            RequestedProvider = provider;
            return Task.FromResult(IsrcResult);
        }

        public Task<Uri?> ReadByTrackMetadataAsync(
            string artistName,
            string trackTitle,
            ProviderName provider,
            CancellationToken cancellationToken)
        {
            RequestedArtistName = artistName;
            RequestedTrackTitle = trackTitle;
            RequestedProvider = provider;
            return Task.FromResult(MetadataResult);
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

    public sealed class IsrcInnerHandlerFake : IHandler<LookupStreamingLocationByIsrcMessage>
    {
        public int Calls { get; private set; }

        public Exception? ExceptionToThrow { get; set; }

        public Task Handle(LookupStreamingLocationByIsrcMessage request, CancellationToken cancellationToken = default)
        {
            Calls++;
            if (ExceptionToThrow is not null)
            {
                throw ExceptionToThrow;
            }

            return Task.CompletedTask;
        }
    }

    public sealed class MetadataInnerHandlerFake : IHandler<LookupStreamingLocationByTrackMetadataMessage>
    {
        public int Calls { get; private set; }

        public Exception? ExceptionToThrow { get; set; }

        public Task Handle(LookupStreamingLocationByTrackMetadataMessage request, CancellationToken cancellationToken = default)
        {
            Calls++;
            if (ExceptionToThrow is not null)
            {
                throw ExceptionToThrow;
            }

            return Task.CompletedTask;
        }
    }
}
