using Soundtrail.Adapters.Timing;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery.Messages;
using Soundtrail.Services.Enrichment.Worker.Features.LookupStreamingLocationByIsrc;
using Soundtrail.Services.Enrichment.Worker.Features.LookupStreamingLocationByTrackMetadata;
using Soundtrail.Services.Enrichment.Worker.Infrastructure.Idempotency.Storage;
using Soundtrail.Services.Enrichment.Worker.Shared.Execution;
using Soundtrail.Services.Enrichment.Worker.Shared.ExecutionAdmission;
using Soundtrail.Services.Enrichment.Worker.Shared.StreamingLocations;
using Soundtrail.Services.Tests.Unit.Sociable.Infrastructure.Fakes;

namespace Soundtrail.Services.Tests.Unit.Worker.LookupStreamingLocations;

internal sealed class LookupStreamingLocationsUnitTestEnvironment
{
    private LookupStreamingLocationsUnitTestEnvironment()
    {
        Clock = new ClockFake(new DateTimeOffset(2026, 7, 20, 11, 45, 0, TimeSpan.Zero));
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

    public AdmittedLookupHandlerDecorator<LookupStreamingLocationByIsrcMessage> CreateIsrcAdmissionSubject(
        IHandler<LookupStreamingLocationByIsrcMessage>? inner = null) =>
        new(inner ?? IsrcInnerHandler, new LookupStreamingLocationByIsrcDecoratorMetadata(), CommandBus, AdmissionPort, Clock);

    public AdmittedLookupHandlerDecorator<LookupStreamingLocationByTrackMetadataMessage> CreateMetadataAdmissionSubject(
        IHandler<LookupStreamingLocationByTrackMetadataMessage>? inner = null) =>
        new(inner ?? MetadataInnerHandler, new LookupStreamingLocationByTrackMetadataDecoratorMetadata(), CommandBus, AdmissionPort, Clock);

    public IdempotentLookupHandlerDecorator<LookupStreamingLocationByIsrcMessage> CreateIsrcIdempotencySubject(
        IHandler<LookupStreamingLocationByIsrcMessage>? inner = null) =>
        new(inner ?? IsrcInnerHandler, new LookupStreamingLocationByIsrcDecoratorMetadata(), ReceiptStore, CommandBus, Clock);

    public IdempotentLookupHandlerDecorator<LookupStreamingLocationByTrackMetadataMessage> CreateMetadataIdempotencySubject(
        IHandler<LookupStreamingLocationByTrackMetadataMessage>? inner = null) =>
        new(inner ?? MetadataInnerHandler, new LookupStreamingLocationByTrackMetadataDecoratorMetadata(), ReceiptStore, CommandBus, Clock);

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

    public sealed class IsrcInnerHandlerFake : IHandler<LookupStreamingLocationByIsrcMessage>
    {
        public int Calls { get; private set; }

        public Exception? ExceptionToThrow { get; set; }

        public Task Handle(IncomingMessage<LookupStreamingLocationByIsrcMessage> context, CancellationToken cancellationToken = default)
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

        public Task Handle(IncomingMessage<LookupStreamingLocationByTrackMetadataMessage> context, CancellationToken cancellationToken = default)
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
