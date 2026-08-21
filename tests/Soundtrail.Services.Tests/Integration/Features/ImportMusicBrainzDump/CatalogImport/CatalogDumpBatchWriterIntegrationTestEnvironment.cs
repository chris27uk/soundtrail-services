using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Raven.Client.Documents;
using Soundtrail.Adapters.TypeRegistry;
using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Contracts.Persistence;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Albums;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Catalog.MusicBrainzDumpImport;
using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Domain.Common;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.ImportCatalogShard.Adapters;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.ImportCatalogShard.Ports;
using Soundtrail.Services.Tests.Integration.Shared.Infrastructure;
using Soundtrail.Services.Tests.Unit.Sociable.Infrastructure.Fakes;

namespace Soundtrail.Services.Tests.Integration.Features.ImportMusicBrainzDump.CatalogImport;

internal sealed class CatalogDumpBatchWriterIntegrationTestEnvironment : IAsyncDisposable
{
    private CatalogDumpBatchWriterIntegrationTestEnvironment(
        IDocumentStore documentStore,
        CommandBusFake commandBus,
        CatalogDumpBatchWriter subject,
        ArtistId artistId,
        AlbumId albumId,
        TrackId trackId,
        string displayArtistName,
        string displayAlbumTitle)
    {
        DocumentStore = documentStore;
        CommandBus = commandBus;
        Subject = subject;
        ArtistId = artistId;
        AlbumId = albumId;
        TrackId = trackId;
        DisplayArtistName = displayArtistName;
        DisplayAlbumTitle = displayAlbumTitle;
    }

    public IDocumentStore DocumentStore { get; }

    public CommandBusFake CommandBus { get; }

    public CatalogDumpBatchWriter Subject { get; }

    public ArtistId ArtistId { get; }

    public AlbumId AlbumId { get; }

    public TrackId TrackId { get; }

    public string DisplayArtistName { get; }

    public string DisplayAlbumTitle { get; }

    public static CatalogDumpBatchWriterIntegrationTestEnvironment Create()
    {
        var isolation = EmbeddedRavenTestServer.NewIsolationKey();
        var artistName = $"Neon Harbour {isolation}";
        var albumTitle = $"Glass Cities {isolation}";
        var artistId = ArtistId.From($"dump-flush-artist-{isolation}");
        var albumId = AlbumId.From(artistId.Value, $"rg-{isolation}");
        var trackId = TrackId.TryCreate(
            artistName,
            albumTitle,
            albumTitle,
            new DateOnly(2024, 6, 23),
            "album") switch
        {
            TrackIdCreateResult.Success success => success.Value,
            TrackIdCreateResult.Failure failure => throw new InvalidOperationException(failure.Reason),
            _ => throw new InvalidOperationException("Unexpected TrackId creation result.")
        };

        var documentStore = EmbeddedRavenTestServer.CreateDocumentStore();
        var commandBus = new CommandBusFake();
        var subject = new CatalogDumpBatchWriter(
            documentStore,
            TypeTranslationRegistry.Default,
            commandBus,
            Options.Create(new MusicBrainzDumpOptions()),
            NullLogger<CatalogDumpBatchWriter>.Instance);

        return new CatalogDumpBatchWriterIntegrationTestEnvironment(
            documentStore,
            commandBus,
            subject,
            artistId,
            albumId,
            trackId,
            artistName,
            albumTitle);
    }

    public async Task FlushArtistAlbumAndTrackAsync(DateTimeOffset? dumpObservedAt = null)
    {
        var observedAt = dumpObservedAt ?? DateTimeOffset.Parse("2026-08-10T00:00:00Z");
        var artist = new Artist
        {
            Id = ArtistId,
            Name = ArtistName.From(DisplayArtistName),
            SourceSystemIds = SourceSystemIdSet.FromLegacyMusicBrainz($"mbid-artist-{ArtistId.Value}")
        };
        var album = new Album(
            AlbumId,
            DisplayAlbumTitle,
            SourceSystemIdSet.FromLegacyMusicBrainz($"mbid-rg-{AlbumId.StableValue}"),
            new DateOnly(2024, 6, 23),
            null,
            observedAt);
        var track = new Track(TrackId)
        {
            Title = DisplayAlbumTitle,
            ArtistName = DisplayArtistName,
            AlbumTitle = DisplayAlbumTitle,
            AlbumId = AlbumId.StableValue,
            UpdatedAt = observedAt
        };
        SourceSystemIdSet.UnionWith(
            track.SourceSystemIds,
            SourceSystemIdSet.FromLegacyMusicBrainz($"mbid-rec-{TrackId.Value}"));

        var touched = await Subject.AppendEventsAsync(
            [
                new ArtistDumpBatchItem(artist),
                new AlbumDumpBatchItem(album),
                new TrackDumpBatchItem(track)
            ],
            observedAt,
            CancellationToken.None);
        await Subject.ProjectArtistsAsync(touched, observedAt, CancellationToken.None);
    }

    public async ValueTask DisposeAsync()
    {
        await EmbeddedRavenTestServer.DeleteDocumentsAsync(
            DocumentStore,
            [
                CatalogArtistRecordDto.GetDocumentId(ArtistId.Value),
                CatalogArtistAlbumsRecordDto.GetDocumentId(ArtistId.Value),
                CatalogArtistTracksRecordDto.GetDocumentId(ArtistId.Value),
                CatalogAlbumRecordDto.GetDocumentId(AlbumId.StableValue),
                CatalogAlbumTracksRecordDto.GetDocumentId(AlbumId.StableValue),
                CatalogTrackRecordDto.GetDocumentId(TrackId.Value),
                CatalogSearchCandidateRecordDto.GetDocumentId(TrackId.Value)
            ]);
    }
}
