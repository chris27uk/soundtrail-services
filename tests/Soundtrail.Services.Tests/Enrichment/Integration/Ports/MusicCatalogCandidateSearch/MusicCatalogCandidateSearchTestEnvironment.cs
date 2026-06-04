using Raven.Client.Documents;
using Raven.Client.Documents.Indexes;
using Soundtrail.Services.Enrichment.Shared.Search;
using Soundtrail.Services.Enrichment.Worker.Infrastructure.Raven;
using Soundtrail.Services.Tests.Api.Integration.Infrastructure;
using System.Reflection;

namespace Soundtrail.Services.Tests.Enrichment.Integration.Ports.MusicCatalogCandidateSearch.RavenEmbedded;

internal sealed class MusicCatalogCandidateSearchTestEnvironment : IDisposable
{
    private readonly RavenEmbeddedTestDatabase raven;

    private MusicCatalogCandidateSearchTestEnvironment(
        IMusicCatalogCandidateSearch search,
        RavenEmbeddedTestDatabase raven)
    {
        Search = search;
        this.raven = raven;
    }

    public IMusicCatalogCandidateSearch Search { get; }

    public static MusicCatalogCandidateSearchTestEnvironment Create()
    {
        var raven = RavenEmbeddedTestDatabase.Create();
        ExecuteTrackCatalogueIndex(raven.Store);
        return new MusicCatalogCandidateSearchTestEnvironment(
            new RavenMusicCatalogCandidateSearch(raven.Store),
            raven);
    }

    public void Seed(string musicCatalogId, string searchText)
    {
        using var session = raven.Store.OpenSession();
        session.Advanced.WaitForIndexesAfterSaveChanges();

        var document = Activator.CreateInstance(
            RavenTrackDocumentType,
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
            binder: null,
            args: null,
            culture: null)!;

        Set(document, "Id", $"track-catalogue/{musicCatalogId}");
        Set(document, "Title", "Fixture Track");
        Set(document, "Artist", "Fixture Artist");
        Set(document, "SearchText", searchText);
        Set(document, "Isrc", null);
        Set(document, "Mbid", null);
        Set(document, "AppleId", null);
        Set(document, "SpotifyId", null);
        Set(document, "DurationMs", null);

        session.Store(document);
        session.SaveChanges();
    }

    public void Dispose() => raven.Dispose();

    private static readonly Type RavenTrackDocumentType = typeof(RavenMusicCatalogCandidateSearch).Assembly
        .GetType("Soundtrail.Services.Enrichment.Worker.Infrastructure.Raven.Documents.RavenTrackDocument", throwOnError: true)!;

    private static readonly Type TrackCatalogueIndexType = typeof(RavenMusicCatalogCandidateSearch).Assembly
        .GetType("Soundtrail.Services.Enrichment.Worker.Infrastructure.Raven.Indexes.TrackCatalogue_BySearchText", throwOnError: true)!;

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

    private static void Set(object target, string propertyName, object? value) =>
        RavenTrackDocumentType
            .GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
            .SetValue(target, value);
}
