using Raven.Client.Documents;
using Raven.Client.Documents.Indexes;
using Soundtrail.Contracts;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.JustInTimeScheduling.Adapters;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.JustInTimeScheduling.Adapters.Documents;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Search;
using Soundtrail.Services.Tests.Integration.Api.Infrastructure;
using System.Reflection;

namespace Soundtrail.Services.Tests.Integration.Enrichment.Ports.MusicCatalogCandidateSearch;

internal sealed class MusicCatalogCandidateSearchTestEnvironment : IDisposable
{
    private readonly RavenEmbeddedTestDatabase? raven;
    private readonly Action<string, string, string?, string?, string?, string?, string?> seed;

    private MusicCatalogCandidateSearchTestEnvironment(
        IMusicCatalogCandidateSearch search,
        Action<string, string, string?, string?, string?, string?, string?> seed,
        RavenEmbeddedTestDatabase? raven)
    {
        Search = search;
        this.seed = seed;
        this.raven = raven;
    }

    public IMusicCatalogCandidateSearch Search { get; }

    public static MusicCatalogCandidateSearchTestEnvironment Create(MusicCatalogCandidateSearchPortMode mode)
    {
        return mode switch
        {
            MusicCatalogCandidateSearchPortMode.InProcessFake => CreateFake(),
            MusicCatalogCandidateSearchPortMode.RavenEmbedded => CreateRavenEmbedded(),
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
        };
    }

    public void Seed(
        string musicCatalogId,
        string searchText,
        string? title = null,
        string? artist = null,
        string? albumTitle = null,
        string? isrc = null,
        string? mbid = null) =>
        this.seed(musicCatalogId, searchText, title, artist, albumTitle, isrc, mbid);

    public void Dispose() => this.raven?.Dispose();

    private static MusicCatalogCandidateSearchTestEnvironment CreateFake()
    {
        var fake = new FakeCandidateSearchPort();
        return new MusicCatalogCandidateSearchTestEnvironment(
            fake,
            fake.Seed,
            raven: null);
    }

    private static MusicCatalogCandidateSearchTestEnvironment CreateRavenEmbedded()
    {
        var raven = RavenEmbeddedTestDatabase.Create();
        ExecuteTrackCatalogueIndex(raven.Store);
        return new MusicCatalogCandidateSearchTestEnvironment(
            new RavenMusicCatalogCandidateSearch(raven.Store),
            (musicCatalogId, searchText, title, artist, albumTitle, isrc, mbid) => SeedRaven(raven, musicCatalogId, searchText, title, artist, albumTitle, isrc, mbid),
            raven);
    }

    private static void SeedRaven(
        RavenEmbeddedTestDatabase raven,
        string musicCatalogId,
        string searchText,
        string? title,
        string? artist,
        string? albumTitle,
        string? isrc,
        string? mbid)
    {
        using var session = raven.Store.OpenSession();
        session.Advanced.WaitForIndexesAfterSaveChanges();

        var document = new RavenTrackRecordDto
        {
            Id = $"track-catalogue/{musicCatalogId}",
            Title = title ?? "Fixture Track",
            Artist = artist ?? "Fixture Artist",
            NormalizedArtist = Domain.Model.MusicIdentityText.NormalizeFreeText(artist ?? "Fixture Artist"),
            AlbumTitle = albumTitle,
            NormalizedAlbumTitle = Domain.Model.MusicIdentityText.NormalizeFreeText(albumTitle),
            SearchText = searchText,
            Isrc = isrc,
            NormalizedIsrc = Domain.Model.MusicIdentityText.NormalizeCompact(isrc),
            Mbid = mbid,
            NormalizedMbid = Domain.Model.MusicIdentityText.NormalizeCompact(mbid),
            AppleId = null,
            SpotifyId = null,
            DurationMs = null
        };

        session.Store(document);
        session.SaveChanges();
    }

    private static readonly Type TrackCatalogueIndexType = typeof(RavenMusicCatalogCandidateSearch).Assembly
        .GetType("Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.JustInTimeScheduling.Adapters.Indexes.TrackCatalogue_BySearchText", throwOnError: true)!;

    private static void ExecuteTrackCatalogueIndex(IDocumentStore store)
    {
        var index = (AbstractIndexCreationTask)Activator.CreateInstance(
            TrackCatalogueIndexType,
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
            binder: null,
            args: null,
            culture: null)!;

        index.Execute(store);
    }
}
