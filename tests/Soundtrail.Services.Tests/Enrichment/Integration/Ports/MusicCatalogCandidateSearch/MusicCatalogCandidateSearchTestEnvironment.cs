using Raven.Client.Documents;
using Raven.Client.Documents.Indexes;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Infrastructure.Raven;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Infrastructure.Raven.Documents;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Search;
using Soundtrail.Services.Tests.Api.Integration.Infrastructure;
using System.Reflection;

namespace Soundtrail.Services.Tests.Enrichment.Integration.Ports.MusicCatalogCandidateSearch;

internal sealed class MusicCatalogCandidateSearchTestEnvironment : IDisposable
{
    private readonly RavenEmbeddedTestDatabase? raven;
    private readonly Action<string, string> seed;

    private MusicCatalogCandidateSearchTestEnvironment(
        IMusicCatalogCandidateSearch search,
        Action<string, string> seed,
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

    public void Seed(string musicCatalogId, string searchText) => this.seed(musicCatalogId, searchText);

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
            (musicCatalogId, searchText) => SeedRaven(raven, musicCatalogId, searchText),
            raven);
    }

    private static void SeedRaven(RavenEmbeddedTestDatabase raven, string musicCatalogId, string searchText)
    {
        using var session = raven.Store.OpenSession();
        session.Advanced.WaitForIndexesAfterSaveChanges();

        var document = new RavenTrackDocument
        {
            Id = $"track-catalogue/{musicCatalogId}",
            Title = "Fixture Track",
            Artist = "Fixture Artist",
            SearchText = searchText,
            Isrc = null,
            Mbid = null,
            AppleId = null,
            SpotifyId = null,
            DurationMs = null
        };

        session.Store(document);
        session.SaveChanges();
    }

    private static readonly Type TrackCatalogueIndexType = typeof(RavenMusicCatalogCandidateSearch).Assembly
        .GetType("Soundtrail.Services.Enrichment.DiscoveryPlanner.Infrastructure.Raven.Indexes.TrackCatalogue_BySearchText", throwOnError: true)!;

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
