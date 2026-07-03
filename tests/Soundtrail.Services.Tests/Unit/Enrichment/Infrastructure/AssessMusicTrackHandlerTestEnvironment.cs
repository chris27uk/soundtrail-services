using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Commands;
using Soundtrail.Domain.Discovery.Events;
using Soundtrail.Domain.Search;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnAssessMusicCatalogItem;
using Soundtrail.Services.Enrichment.Orchestrator.Shared.Prioritisation;
using Soundtrail.Services.Enrichment.Orchestrator.Shared.Search;

namespace Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

internal sealed class AssessMusicCatalogItemHandlerTestEnvironment
{
    private readonly CatalogSearchDiscoveryRepositoryFake discoveryRepository;
    private readonly LocalMusicTrackSearchFake localSearch;

    private AssessMusicCatalogItemHandlerTestEnvironment()
    {
        discoveryRepository = new CatalogSearchDiscoveryRepositoryFake();
        localSearch = new LocalMusicTrackSearchFake();
        Handler = new AssessMusicCatalogItemHandler(
            discoveryRepository,
            new DiscoveryPriorityPolicy(),
            localSearch);
    }

    public AssessMusicCatalogItemHandler Handler { get; }

    public static AssessMusicCatalogItemHandlerTestEnvironment Create() => new();

    public void SeedDiscoveryRequested(MusicSearchCriteria searchCriteria)
    {
        discoveryRepository.Seed(
            searchCriteria,
            new DiscoveryRequested(
                searchCriteria,
                null,
                1,
                10,
                Clock,
                CorrelationId.From("corr-1")));
    }

    public void SeedCandidateIdentified(
        MusicSearchCriteria searchCriteria,
        MusicCatalogId musicCatalogId,
        int trustLevel = 1,
        int riskScore = 10)
    {
        discoveryRepository.Seed(
            searchCriteria,
            new CatalogCandidateIdentified(
                searchCriteria,
                musicCatalogId,
                trustLevel,
                riskScore,
                Clock,
                CorrelationId.From("corr-1")));
    }

    public void SeedDiscoveryDeferred(
        MusicSearchCriteria searchCriteria,
        DateTimeOffset? earliestExpectedCompletionAt = null)
    {
        discoveryRepository.Seed(
            searchCriteria,
            new DiscoveryDeferred(
                searchCriteria,
                true,
                60,
                earliestExpectedCompletionAt ?? Clock.AddSeconds(60),
                "Planner deferred lookup",
                Clock));
    }

    public void SeedPlayableTrack(MusicCatalogId musicCatalogId)
    {
        localSearch.Seed(new LocalMusicTrackSearchResult(
            musicCatalogId,
            "Rare Unknown Song",
            "Test Artist",
            "Rare Album",
            Isrc: null,
            Mbid: null,
            DurationMs: null,
            IsPlayable: true,
            AvailableProviders: [ProviderName.Spotify],
            ReleaseDate: null));
    }

    public IReadOnlyList<IDomainEvent> StoredEvents(MusicSearchCriteria searchCriteria) =>
        discoveryRepository.GetStoredEvents(searchCriteria);

    public IReadOnlyList<IDomainEvent> StoredEvents(KnownCatalogId knownId) =>
        discoveryRepository.GetStoredEvents(knownId);

    public AssessMusicCatalogItemCommand ImmediateCommand(
        MusicSearchCriteria searchCriteria,
        MusicCatalogId musicCatalogId,
        int trustLevel = 1,
        int riskScore = 10) =>
        new(
            AssessMusicCatalogItemCommand.Id(
                new CatalogItemId.Track(TrackId.From(musicCatalogId.Value)),
                CatalogItemResource.ForSearch(searchCriteria),
                Clock),
            CorrelationId.From("corr-1"),
            Clock,
            LookupPriorityBand.Low,
            new CatalogItemId.Track(TrackId.From(musicCatalogId.Value)),
            CatalogItemResource.ForSearch(searchCriteria),
            trustLevel,
            riskScore);

    public AssessMusicCatalogItemCommand BacklogCommand(MusicSearchCriteria searchCriteria, MusicCatalogId musicCatalogId) =>
        new(
            AssessMusicCatalogItemCommand.Id(
                new CatalogItemId.Track(TrackId.From(musicCatalogId.Value)),
                CatalogItemResource.ForSearch(searchCriteria),
                Clock),
            CorrelationId.From("corr-1"),
            Clock,
            LookupPriorityBand.Low,
            new CatalogItemId.Track(TrackId.From(musicCatalogId.Value)),
            CatalogItemResource.ForSearch(searchCriteria));

    public AssessMusicCatalogItemCommand ArtistSearchCommand(MusicSearchCriteria searchCriteria, string artistId = "artist_1") =>
        new(
            AssessMusicCatalogItemCommand.Id(
                new CatalogItemId.Artist(ArtistId.From(artistId)),
                CatalogItemResource.ForSearch(searchCriteria),
                Clock),
            CorrelationId.From("corr-1"),
            Clock,
            LookupPriorityBand.Low,
            new CatalogItemId.Artist(ArtistId.From(artistId)),
            CatalogItemResource.ForSearch(searchCriteria));

    public AssessMusicCatalogItemCommand AlbumSearchCommand(
        MusicSearchCriteria searchCriteria,
        string artistId = "artist_1",
        string albumId = "album_1") =>
        new(
            AssessMusicCatalogItemCommand.Id(
                new CatalogItemId.Album(CatalogAlbumId.From(ArtistId.From(artistId), AlbumId.From(albumId))),
                CatalogItemResource.ForSearch(searchCriteria),
                Clock),
            CorrelationId.From("corr-1"),
            Clock,
            LookupPriorityBand.Low,
            new CatalogItemId.Album(CatalogAlbumId.From(ArtistId.From(artistId), AlbumId.From(albumId))),
            CatalogItemResource.ForSearch(searchCriteria));

    public AssessMusicCatalogItemCommand TrackCatalogItemResourceCommand(
        MusicCatalogId musicCatalogId,
        string resourceTrackId = "track_2") =>
        new(
            AssessMusicCatalogItemCommand.Id(
                new CatalogItemId.Track(TrackId.From(musicCatalogId.Value)),
                CatalogItemResource.ForCatalogItem(new CatalogItemId.Track(TrackId.From(resourceTrackId))),
                Clock),
            CorrelationId.From("corr-1"),
            Clock,
            LookupPriorityBand.Low,
            new CatalogItemId.Track(TrackId.From(musicCatalogId.Value)),
            CatalogItemResource.ForCatalogItem(new CatalogItemId.Track(TrackId.From(resourceTrackId))));

    public AssessMusicCatalogItemCommand ArtistCatalogItemResourceCommand(
        string artistId = "artist_1",
        string resourceArtistId = "artist_parent") =>
        new(
            AssessMusicCatalogItemCommand.Id(
                new CatalogItemId.Artist(ArtistId.From(artistId)),
                CatalogItemResource.ForCatalogItem(new CatalogItemId.Artist(ArtistId.From(resourceArtistId))),
                Clock),
            CorrelationId.From("corr-1"),
            Clock,
            LookupPriorityBand.Low,
            new CatalogItemId.Artist(ArtistId.From(artistId)),
            CatalogItemResource.ForCatalogItem(new CatalogItemId.Artist(ArtistId.From(resourceArtistId))));

    public AssessMusicCatalogItemCommand AlbumCatalogItemResourceCommand(
        string artistId = "artist_1",
        string albumId = "album_1",
        string resourceArtistId = "artist_parent") =>
        new(
            AssessMusicCatalogItemCommand.Id(
                new CatalogItemId.Album(CatalogAlbumId.From(ArtistId.From(artistId), AlbumId.From(albumId))),
                CatalogItemResource.ForCatalogItem(new CatalogItemId.Artist(ArtistId.From(resourceArtistId))),
                Clock),
            CorrelationId.From("corr-1"),
            Clock,
            LookupPriorityBand.Low,
            new CatalogItemId.Album(CatalogAlbumId.From(ArtistId.From(artistId), AlbumId.From(albumId))),
            CatalogItemResource.ForCatalogItem(new CatalogItemId.Artist(ArtistId.From(resourceArtistId))));

    private static readonly DateTimeOffset Clock = new(2026, 6, 28, 12, 0, 0, TimeSpan.Zero);
}
