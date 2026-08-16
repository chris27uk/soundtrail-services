using Soundtrail.Adapters.MusicBrainzDumpFreshness;
using Soundtrail.Domain.Catalog.Albums;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Catalog.Playlists;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Messages;
using Soundtrail.Domain.Search;
using Soundtrail.Services.Enrichment.Orchestrator.Features.Processing.OnLookupWorkReady;
using Soundtrail.Services.Tests.Unit.Sociable.Features.GetAlbumsForArtist.Support;
using Soundtrail.Services.Tests.Unit.Sociable.Features.GetTracksForAlbum.Support;
using Soundtrail.Services.Tests.Unit.Sociable.Features.GetTracksForArtist.Support;
using Soundtrail.Services.Tests.Unit.Sociable.Infrastructure.Fakes;

namespace Soundtrail.Services.Tests.Unit.Solitary.CrossCutting.Orchestrator.OnLookupWorkReady;

internal sealed class LookupWorkReadyHandlerUnitTestEnvironment
{
    private static readonly DateTimeOffset DefaultUtcNow = new(2026, 8, 16, 12, 0, 0, TimeSpan.Zero);

    private readonly LookupWorkReadyHandler subject;

    private LookupWorkReadyHandlerUnitTestEnvironment(
        CommandBusFake commandBus,
        ClockFake clock,
        LookupWorkReadyHandler subject,
        DispatchLookupWork? pendingRequest)
    {
        CommandBus = commandBus;
        Clock = clock;
        this.subject = subject;
        PendingRequest = pendingRequest;
    }

    public CommandBusFake CommandBus { get; }

    public ClockFake Clock { get; }

    public DispatchLookupWork? PendingRequest { get; }

    public static LookupWorkReadyHandlerUnitTestEnvironment Create() =>
        ForDumpStaleCatalogRequiringLiveLookup();

    public static LookupWorkReadyHandlerUnitTestEnvironment ForDumpFreshArtistAlbums(
        DateTimeOffset utcNow = default)
    {
        utcNow = Normalize(utcNow);
        var request = CreateArtistAlbumsRequest(DumpFreshScenarios.ArtistId, utcNow);
        return Compose(
            utcNow,
            new MusicBrainzDumpFreshnessEvaluatorFake()
                .WithArtistAlbumsCatalog(DumpFreshScenarios.MidnightSignalsAlbum(utcNow)),
            request);
    }

    public static LookupWorkReadyHandlerUnitTestEnvironment ForDumpStaleArtistAlbumsRequiringLiveLookup(
        DateTimeOffset utcNow = default)
    {
        utcNow = Normalize(utcNow);
        return Compose(
            utcNow,
            new MusicBrainzDumpFreshnessEvaluatorFake(),
            CreateArtistAlbumsRequest(DumpFreshScenarios.ArtistId, utcNow));
    }

    public static LookupWorkReadyHandlerUnitTestEnvironment ForDumpFreshArtistTracks(
        DateTimeOffset utcNow = default)
    {
        utcNow = Normalize(utcNow);
        return Compose(
            utcNow,
            new MusicBrainzDumpFreshnessEvaluatorFake()
                .WithArtistTracksCatalog(DumpFreshScenarios.GlassCitiesArtistTrack(utcNow)),
            CreateArtistTracksRequest(DumpFreshScenarios.ArtistId, utcNow));
    }

    public static LookupWorkReadyHandlerUnitTestEnvironment ForDumpFreshAlbumTracks(
        DateTimeOffset utcNow = default)
    {
        utcNow = Normalize(utcNow);
        return Compose(
            utcNow,
            new MusicBrainzDumpFreshnessEvaluatorFake()
                .WithAlbumTracksCatalog(DumpFreshScenarios.GlassCitiesAlbumTrack(utcNow)),
            CreateAlbumTracksRequest(DumpFreshScenarios.AlbumId, utcNow));
    }

    public static LookupWorkReadyHandlerUnitTestEnvironment ForDumpStaleCatalogRequiringLiveLookup(
        DateTimeOffset utcNow = default)
    {
        utcNow = Normalize(utcNow);
        var commandBus = new CommandBusFake();
        var dumpFreshness = new MusicBrainzDumpFreshnessEvaluatorFake();
        var clock = new ClockFake(utcNow);
        return new LookupWorkReadyHandlerUnitTestEnvironment(
            commandBus,
            clock,
            new LookupWorkReadyHandler(dumpFreshness, clock, commandBus),
            pendingRequest: null);
    }

    public LookupWorkReadyHandler CreateSubject() => subject;

    public Task HandleLookupAsync(CancellationToken cancellationToken = default)
    {
        if (PendingRequest is null)
        {
            throw new InvalidOperationException("No pending lookup request was configured for this environment.");
        }

        return subject.Handle(PendingRequest, cancellationToken);
    }

    public Task HandleAsync(DispatchLookupWork request, CancellationToken cancellationToken = default) =>
        subject.Handle(request, cancellationToken);

    public static DispatchLookupWork CreateSearchRequest(string commandId = "cmd-search") =>
        new(
            new EnrichmentTarget.SearchForUnknownCatalogItem(new SearchCriteria("u2", SearchType.Artist)),
            LookupPriorityBand.High,
            MessageId.For(commandId),
            CorrelationId.From($"corr:{commandId}"),
            new DateTimeOffset(2026, 7, 18, 9, 10, 0, TimeSpan.Zero));

    public static DispatchLookupWork CreateStreamingLocationRequest(string commandId = "cmd-streaming") =>
        new(
            Work.EnrichTrackStreamingLocation(TestTrackIds.Create("track-2901")),
            LookupPriorityBand.Low,
            MessageId.For(commandId),
            CorrelationId.From($"corr:{commandId}"),
            new DateTimeOffset(2026, 7, 18, 9, 11, 0, TimeSpan.Zero));

    public static DispatchLookupWork CreatePlaylistRequest() =>
        new(
            Work.DiscoverPlaylistTracks(PlaylistId.FromPlaylistName("roadtrip")),
            LookupPriorityBand.Low,
            MessageId.For("cmd-playlist"),
            CorrelationId.From("corr-playlist"),
            new DateTimeOffset(2026, 7, 18, 9, 12, 0, TimeSpan.Zero));

    public static DispatchLookupWork CreateArtistAlbumsRequest(
        ArtistId? artistId = null,
        DateTimeOffset utcNow = default)
    {
        utcNow = Normalize(utcNow);
        var id = artistId ?? DumpFreshScenarios.ArtistId;
        return new DispatchLookupWork(
            Work.DiscoverArtistAlbums(id),
            LookupPriorityBand.High,
            MessageId.For("cmd-artist-albums"),
            CorrelationId.From("corr-artist-albums"),
            utcNow);
    }

    public static DispatchLookupWork CreateArtistTracksRequest(
        ArtistId? artistId = null,
        DateTimeOffset utcNow = default)
    {
        utcNow = Normalize(utcNow);
        var id = artistId ?? DumpFreshScenarios.ArtistId;
        return new DispatchLookupWork(
            Work.DiscoverArtistTracks(id),
            LookupPriorityBand.High,
            MessageId.For("cmd-artist-tracks"),
            CorrelationId.From("corr-artist-tracks"),
            utcNow);
    }

    public static DispatchLookupWork CreateAlbumTracksRequest(
        AlbumId? albumId = null,
        DateTimeOffset utcNow = default)
    {
        utcNow = Normalize(utcNow);
        var id = albumId ?? DumpFreshScenarios.AlbumId;
        return new DispatchLookupWork(
            Work.DiscoverAlbumTracks(id),
            LookupPriorityBand.High,
            MessageId.For("cmd-album-tracks"),
            CorrelationId.From("corr-album-tracks"),
            utcNow);
    }

    private static LookupWorkReadyHandlerUnitTestEnvironment Compose(
        DateTimeOffset utcNow,
        MusicBrainzDumpFreshnessEvaluatorFake dumpFreshness,
        DispatchLookupWork request)
    {
        var commandBus = new CommandBusFake();
        var clock = new ClockFake(utcNow);
        return new LookupWorkReadyHandlerUnitTestEnvironment(
            commandBus,
            clock,
            new LookupWorkReadyHandler(dumpFreshness, clock, commandBus),
            request);
    }

    private static DateTimeOffset Normalize(DateTimeOffset utcNow) =>
        utcNow == default ? DefaultUtcNow : utcNow;

    private static class DumpFreshScenarios
    {
        public static ArtistId ArtistId { get; } = ArtistId.From("artist-aurora");

        public static AlbumId AlbumId { get; } = AlbumId.From(ArtistId.Value, "rg-midnight");

        public static CatalogDiscoveryEntry MidnightSignalsAlbum(DateTimeOffset catalogUpdatedAt) =>
            LookupDataCompleteArtistAlbum.Create(
                ArtistId,
                "Midnight Signals",
                new DateOnly(2023, 11, 10),
                catalogUpdatedAt,
                sourceAlbumId: "rg-midnight").CatalogEntry;

        public static CatalogDiscoveryEntry GlassCitiesArtistTrack(DateTimeOffset catalogUpdatedAt) =>
            LookupDataCompleteArtistTrack.Create(
                ArtistId,
                "Aurora",
                "Glass Cities",
                "Midnight Signals",
                new DateOnly(2023, 11, 10),
                "album",
                180_000,
                catalogUpdatedAt).CatalogEntry;

        public static CatalogDiscoveryEntry GlassCitiesAlbumTrack(DateTimeOffset catalogUpdatedAt) =>
            LookupDataCompleteAlbumTrack.Create(
                AlbumId,
                "Aurora",
                "Glass Cities",
                "Midnight Signals",
                new DateOnly(2023, 11, 10),
                "album",
                180_000,
                catalogUpdatedAt).CatalogEntry;
    }
}
