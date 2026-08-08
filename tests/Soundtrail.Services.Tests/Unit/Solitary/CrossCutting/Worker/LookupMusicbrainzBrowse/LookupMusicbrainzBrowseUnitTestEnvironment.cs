using Soundtrail.Adapters.Timing;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Albums;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Messages;
using Soundtrail.Services.Enrichment.Worker.Features.LookupMusicbrainzAlbumTracks;
using Soundtrail.Services.Enrichment.Worker.Features.LookupMusicbrainzArtistAlbums;
using Soundtrail.Services.Enrichment.Worker.Features.LookupMusicbrainzArtistTracks;
using Soundtrail.Services.Enrichment.Worker.Infrastructure.Idempotency.Storage;
using Soundtrail.Services.Enrichment.Worker.Shared.Execution;
using Soundtrail.Services.Enrichment.Worker.Shared.ExecutionAdmission;
using Soundtrail.Services.Enrichment.Worker.Shared.MusicMetadata;
using Soundtrail.Services.Tests.Unit.Sociable.Features.GetAlbumsForArtist;
using Soundtrail.Services.Tests.Unit.Sociable.Features.GetTracksForAlbum;
using Soundtrail.Services.Tests.Unit.Sociable.Features.GetTracksForArtist;
using Soundtrail.Services.Tests.Unit.Sociable.Infrastructure.Fakes;

namespace Soundtrail.Services.Tests.Unit.Worker.LookupMusicbrainzBrowse;

internal sealed class LookupMusicbrainzBrowseUnitTestEnvironment
{
    private LookupMusicbrainzBrowseUnitTestEnvironment()
    {
        Clock = new ClockFake(new DateTimeOffset(2026, 7, 20, 11, 45, 0, TimeSpan.Zero));
        CommandBus = new CommandBusFake();
        ReadAlbumsByArtistIdPort = new ReadAlbumsByArtistIdPortFake();
        ReadTracksByArtistIdPort = new ReadTracksByArtistIdPortFake();
        ReadTracksByAlbumIdPort = new ReadTracksByAlbumIdPortFake();
        AdmissionPort = new LookupExecutionAdmissionPortFake();
        ReceiptStore = new LookupExecutionReceiptStoreFake();
        ArtistAlbumsInnerHandler = new ArtistAlbumsInnerHandlerFake();
        ArtistTracksInnerHandler = new ArtistTracksInnerHandlerFake();
        AlbumTracksInnerHandler = new AlbumTracksInnerHandlerFake();
    }

    public ClockFake Clock { get; }
    public CommandBusFake CommandBus { get; }
    public ReadAlbumsByArtistIdPortFake ReadAlbumsByArtistIdPort { get; }
    public ReadTracksByArtistIdPortFake ReadTracksByArtistIdPort { get; }
    public ReadTracksByAlbumIdPortFake ReadTracksByAlbumIdPort { get; }
    public LookupExecutionAdmissionPortFake AdmissionPort { get; }
    public LookupExecutionReceiptStoreFake ReceiptStore { get; }
    public ArtistAlbumsInnerHandlerFake ArtistAlbumsInnerHandler { get; }
    public ArtistTracksInnerHandlerFake ArtistTracksInnerHandler { get; }
    public AlbumTracksInnerHandlerFake AlbumTracksInnerHandler { get; }

    public static LookupMusicbrainzBrowseUnitTestEnvironment Create() => new();

    public AdmittedLookupHandlerDecorator<LookupMusicbrainzArtistAlbumsMessage> CreateArtistAlbumsAdmissionSubject(
        IHandler<LookupMusicbrainzArtistAlbumsMessage>? inner = null) =>
        new(inner ?? ArtistAlbumsInnerHandler, new LookupMusicbrainzArtistAlbumsDecoratorMetadata(), CommandBus, AdmissionPort, Clock);

    public AdmittedLookupHandlerDecorator<LookupMusicbrainzArtistTracksMessage> CreateArtistTracksAdmissionSubject(
        IHandler<LookupMusicbrainzArtistTracksMessage>? inner = null) =>
        new(inner ?? ArtistTracksInnerHandler, new LookupMusicbrainzArtistTracksDecoratorMetadata(), CommandBus, AdmissionPort, Clock);

    public AdmittedLookupHandlerDecorator<LookupMusicbrainzAlbumTracksMessage> CreateAlbumTracksAdmissionSubject(
        IHandler<LookupMusicbrainzAlbumTracksMessage>? inner = null) =>
        new(inner ?? AlbumTracksInnerHandler, new LookupMusicbrainzAlbumTracksDecoratorMetadata(), CommandBus, AdmissionPort, Clock);

    public IdempotentLookupHandlerDecorator<LookupMusicbrainzArtistAlbumsMessage> CreateArtistAlbumsIdempotencySubject(
        IHandler<LookupMusicbrainzArtistAlbumsMessage>? inner = null) =>
        new(inner ?? ArtistAlbumsInnerHandler, new LookupMusicbrainzArtistAlbumsDecoratorMetadata(), ReceiptStore, CommandBus, Clock);

    public IdempotentLookupHandlerDecorator<LookupMusicbrainzArtistTracksMessage> CreateArtistTracksIdempotencySubject(
        IHandler<LookupMusicbrainzArtistTracksMessage>? inner = null) =>
        new(inner ?? ArtistTracksInnerHandler, new LookupMusicbrainzArtistTracksDecoratorMetadata(), ReceiptStore, CommandBus, Clock);

    public IdempotentLookupHandlerDecorator<LookupMusicbrainzAlbumTracksMessage> CreateAlbumTracksIdempotencySubject(
        IHandler<LookupMusicbrainzAlbumTracksMessage>? inner = null) =>
        new(inner ?? AlbumTracksInnerHandler, new LookupMusicbrainzAlbumTracksDecoratorMetadata(), ReceiptStore, CommandBus, Clock);

    public LookupMusicbrainzArtistAlbumsMessage CreateArtistAlbumsRequest(string artistId = "artist-mb-1") =>
        new(
            MessageId.For("cmd-mb-artist-albums"),
            CorrelationId.From("corr:cmd-mb-artist-albums"),
            new DateTimeOffset(2026, 7, 20, 10, 30, 0, TimeSpan.Zero),
            LookupPriorityBand.High,
            ArtistId.From(artistId));

    public LookupMusicbrainzArtistTracksMessage CreateArtistTracksRequest(string artistId = "artist-mb-1") =>
        new(
            MessageId.For("cmd-mb-artist-tracks"),
            CorrelationId.From("corr:cmd-mb-artist-tracks"),
            new DateTimeOffset(2026, 7, 20, 10, 30, 0, TimeSpan.Zero),
            LookupPriorityBand.High,
            ArtistId.From(artistId));

    public LookupMusicbrainzAlbumTracksMessage CreateAlbumTracksRequest(string artistId = "artist-mb-1", string releaseId = "release-mb-1") =>
        new(
            MessageId.For("cmd-mb-album-tracks"),
            CorrelationId.From("corr:cmd-mb-album-tracks"),
            new DateTimeOffset(2026, 7, 20, 10, 30, 0, TimeSpan.Zero),
            LookupPriorityBand.High,
            AlbumId.From(artistId, releaseId));

    public static IReadOnlyList<CatalogDiscoveryEntry> CreateAlbumEntries()
    {
        var artistId = ArtistId.From("artist-mb-1");
        return
        [
            new CatalogDiscoveryEntry(
                artistId,
                new CatalogItem.MusicAlbum(
                    new Album(
                        AlbumId.From(artistId.Value, "release-mb-1"),
                        "Rare Release",
                        "release-mb-1",
                        new DateOnly(2026, 1, 2),
                        artworkUrl: null,
                        updatedAt: new DateTimeOffset(2026, 7, 20, 11, 0, 0, TimeSpan.Zero))))
        ];
    }

    public static IReadOnlyList<CatalogDiscoveryEntry> CreateArtistTrackEntries()
    {
        var artistId = ArtistId.From("artist-mb-1");
        var trackId = TestTrackIds.Create("mb-artist-track-1");
        var track = new Track(trackId)
        {
            Title = "Rare Unknown Song",
            ArtistName = "Test Artist",
            Isrc = "isrc-rare-1",
            UpdatedAt = new DateTimeOffset(2026, 7, 20, 11, 0, 0, TimeSpan.Zero)
        };

        return [new CatalogDiscoveryEntry(artistId, new CatalogItem.MusicTrack(track))];
    }

    public static IReadOnlyList<CatalogDiscoveryEntry> CreateAlbumTrackEntries()
    {
        var artistId = ArtistId.From("artist-mb-1");
        var albumId = AlbumId.From(artistId.Value, "release-mb-1");
        var trackId = TestTrackIds.Create("Album Track 1");
        var track = new Track(trackId)
        {
            Title = "Album Track 1",
            ArtistName = "Test Artist",
            AlbumId = albumId.StableValue,
            AlbumTitle = "Rare Release",
            Isrc = "isrc-album-1",
            UpdatedAt = new DateTimeOffset(2026, 7, 20, 11, 0, 0, TimeSpan.Zero)
        };

        return [new CatalogDiscoveryEntry(artistId, new CatalogItem.MusicTrack(track))];
    }

    public sealed class ArtistAlbumsInnerHandlerFake : IHandler<LookupMusicbrainzArtistAlbumsMessage>
    {
        public int Calls { get; private set; }
        public Exception? ExceptionToThrow { get; set; }

        public Task Handle(IncomingMessage<LookupMusicbrainzArtistAlbumsMessage> context, CancellationToken cancellationToken = default)
        {
            Calls++;
            if (ExceptionToThrow is not null)
            {
                throw ExceptionToThrow;
            }

            return Task.CompletedTask;
        }
    }

    public sealed class ArtistTracksInnerHandlerFake : IHandler<LookupMusicbrainzArtistTracksMessage>
    {
        public int Calls { get; private set; }
        public Exception? ExceptionToThrow { get; set; }

        public Task Handle(IncomingMessage<LookupMusicbrainzArtistTracksMessage> context, CancellationToken cancellationToken = default)
        {
            Calls++;
            if (ExceptionToThrow is not null)
            {
                throw ExceptionToThrow;
            }

            return Task.CompletedTask;
        }
    }

    public sealed class AlbumTracksInnerHandlerFake : IHandler<LookupMusicbrainzAlbumTracksMessage>
    {
        public int Calls { get; private set; }
        public Exception? ExceptionToThrow { get; set; }

        public Task Handle(IncomingMessage<LookupMusicbrainzAlbumTracksMessage> context, CancellationToken cancellationToken = default)
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
