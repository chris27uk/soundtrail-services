using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Albums;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Messages;
using Soundtrail.Services.Enrichment.Worker.Features.LookupMusicbrainzAlbumTracks;
using Soundtrail.Services.Enrichment.Worker.Features.LookupMusicbrainzArtistAlbums;
using Soundtrail.Services.Enrichment.Worker.Features.LookupMusicbrainzArtistTracks;
using Soundtrail.Services.Tests.Unit.Sociable.Features.GetAlbumsForArtist.Support;
using Soundtrail.Services.Tests.Unit.Sociable.Features.GetTracksForAlbum.Support;
using Soundtrail.Services.Tests.Unit.Sociable.Features.GetTracksForArtist.Support;
using Soundtrail.Services.Tests.Unit.Sociable.Infrastructure.Fakes;

namespace Soundtrail.Services.Tests.Unit.Sociable.Worker.MusicBrainzDumpFreshness;

internal sealed class MusicBrainzDumpFreshnessGateSociableTestEnvironment
{
    private static readonly DateTimeOffset DefaultUtcNow = new(2026, 8, 16, 12, 0, 0, TimeSpan.Zero);

    private readonly Func<CancellationToken, Task> handleLookup;
    private readonly Func<int> browseCallCount;
    private readonly CommandBusFake commandBus;

    private MusicBrainzDumpFreshnessGateSociableTestEnvironment(
        Func<CancellationToken, Task> handleLookup,
        Func<int> browseCallCount,
        CommandBusFake commandBus,
        DateTimeOffset utcNow)
    {
        this.handleLookup = handleLookup;
        this.browseCallCount = browseCallCount;
        this.commandBus = commandBus;
        UtcNow = utcNow;
    }

    public DateTimeOffset UtcNow { get; }

    public int MusicBrainzBrowseCallCount => browseCallCount();

    public static MusicBrainzDumpFreshnessGateSociableTestEnvironment ForDumpFreshArtistAlbums(
        DateTimeOffset utcNow = default)
    {
        utcNow = Normalize(utcNow);
        var artistId = DumpFreshScenarios.ArtistId;
        var catalogEntry = DumpFreshScenarios.MidnightSignalsAlbum(utcNow);
        var browse = new ReadAlbumsByArtistIdPortFake();
        var commandBus = new CommandBusFake();
        var subject = new LookupMusicbrainzArtistAlbumsHandler(
            new MusicBrainzDumpFreshnessEvaluatorFake().WithArtistAlbumsCatalog(catalogEntry),
            browse,
            new ClockFake(utcNow),
            commandBus);

        return new MusicBrainzDumpFreshnessGateSociableTestEnvironment(
            ct => subject.Handle(
                new LookupMusicbrainzArtistAlbumsMessage(
                    MessageId.For("cmd-albums"),
                    CorrelationId.From("corr-albums"),
                    utcNow,
                    LookupPriorityBand.High,
                    artistId),
                ct),
            () => browse.ReadCallCount,
            commandBus,
            utcNow);
    }

    public static MusicBrainzDumpFreshnessGateSociableTestEnvironment ForDumpStaleArtistAlbumsRequiringLiveLookup(
        DateTimeOffset utcNow = default)
    {
        utcNow = Normalize(utcNow);
        var artistId = DumpFreshScenarios.ArtistId;
        var browse = new ReadAlbumsByArtistIdPortFake()
            .WithAlbums(artistId, DumpFreshScenarios.LiveLookupAlbum(utcNow));
        var commandBus = new CommandBusFake();
        var subject = new LookupMusicbrainzArtistAlbumsHandler(
            new MusicBrainzDumpFreshnessEvaluatorFake(),
            browse,
            new ClockFake(utcNow),
            commandBus);

        return new MusicBrainzDumpFreshnessGateSociableTestEnvironment(
            ct => subject.Handle(
                new LookupMusicbrainzArtistAlbumsMessage(
                    MessageId.For("cmd-albums-stale"),
                    CorrelationId.From("corr-albums-stale"),
                    utcNow,
                    LookupPriorityBand.High,
                    artistId),
                ct),
            () => browse.ReadCallCount,
            commandBus,
            utcNow);
    }

    public static MusicBrainzDumpFreshnessGateSociableTestEnvironment ForDumpFreshArtistTracks(
        DateTimeOffset utcNow = default)
    {
        utcNow = Normalize(utcNow);
        var artistId = DumpFreshScenarios.ArtistId;
        var catalogEntry = DumpFreshScenarios.GlassCitiesArtistTrack(utcNow);
        var browse = new ReadTracksByArtistIdPortFake();
        var commandBus = new CommandBusFake();
        var subject = new LookupMusicbrainzArtistTracksHandler(
            new MusicBrainzDumpFreshnessEvaluatorFake().WithArtistTracksCatalog(catalogEntry),
            browse,
            new ClockFake(utcNow),
            commandBus);

        return new MusicBrainzDumpFreshnessGateSociableTestEnvironment(
            ct => subject.Handle(
                new LookupMusicbrainzArtistTracksMessage(
                    MessageId.For("cmd-tracks"),
                    CorrelationId.From("corr-tracks"),
                    utcNow,
                    LookupPriorityBand.High,
                    artistId),
                ct),
            () => browse.ReadCallCount,
            commandBus,
            utcNow);
    }

    public static MusicBrainzDumpFreshnessGateSociableTestEnvironment ForDumpFreshAlbumTracks(
        DateTimeOffset utcNow = default)
    {
        utcNow = Normalize(utcNow);
        var albumId = DumpFreshScenarios.AlbumId;
        var catalogEntry = DumpFreshScenarios.GlassCitiesAlbumTrack(utcNow);
        var browse = new ReadTracksByAlbumIdPortFake();
        var commandBus = new CommandBusFake();
        var subject = new LookupMusicbrainzAlbumTracksHandler(
            new MusicBrainzDumpFreshnessEvaluatorFake().WithAlbumTracksCatalog(catalogEntry),
            browse,
            new ClockFake(utcNow),
            commandBus);

        return new MusicBrainzDumpFreshnessGateSociableTestEnvironment(
            ct => subject.Handle(
                new LookupMusicbrainzAlbumTracksMessage(
                    MessageId.For("cmd-album-tracks"),
                    CorrelationId.From("corr-album-tracks"),
                    utcNow,
                    LookupPriorityBand.High,
                    albumId),
                ct),
            () => browse.ReadCallCount,
            commandBus,
            utcNow);
    }

    public Task HandleLookupAsync(CancellationToken cancellationToken = default) =>
        handleLookup(cancellationToken);

    public IReadOnlyList<CatalogDiscoveryEntry> SucceededCatalogEntries()
    {
        var completed = commandBus.SentMessages.OfType<CatalogLookupCompleted>().Should().ContainSingle().Subject;
        var succeeded = completed.Result.Should().BeOfType<LookupResult.Succeeded>().Subject;
        return succeeded.Value.Should().BeOfType<LookedUpData.CatalogEntries>().Subject.Values;
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

        public static CatalogDiscoveryEntry LiveLookupAlbum(DateTimeOffset catalogUpdatedAt) =>
            LookupDataCompleteArtistAlbum.Create(
                ArtistId,
                "Live Lookup Album",
                new DateOnly(2024, 1, 1),
                catalogUpdatedAt,
                sourceAlbumId: "live-rg").CatalogEntry;

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
