using Raven.Client.Documents;
using Raven.Client.Documents.Indexes;
using Soundtrail.Domain.Model;
using Soundtrail.Services.Api.Features.Search.TrackSearch;
using Soundtrail.Services.Api.Infrastructure.Raven;
using Soundtrail.Services.Tests.Integration.Api.Infrastructure;
using System.Reflection;

namespace Soundtrail.Services.Tests.Integration.Api.Ports.TrackSearch;

internal sealed class TrackSearchTestEnvironment : IDisposable
{
    private readonly RavenEmbeddedTestDatabase? raven;
    private readonly Action<SearchResult[]> seed;

    private TrackSearchTestEnvironment(
        ITrackSearchPort search,
        Action<SearchResult[]> seed,
        RavenEmbeddedTestDatabase? raven)
    {
        Search = search;
        this.seed = seed;
        this.raven = raven;
    }

    public ITrackSearchPort Search { get; }

    public static TrackSearchTestEnvironment Create(TrackSearchPortMode mode)
    {
        return mode switch
        {
            TrackSearchPortMode.InProcessFake => CreateFake(),
            TrackSearchPortMode.RavenEmbedded => CreateRavenEmbedded(),
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
        };
    }

    public void Seed(params SearchResult[] results) => this.seed(results);

    public void Dispose() => this.raven?.Dispose();

    private static TrackSearchTestEnvironment CreateFake()
    {
        var fake = new FakeTrackSearchPort();
        return new TrackSearchTestEnvironment(
            fake,
            fake.Seed,
            raven: null);
    }

    private static TrackSearchTestEnvironment CreateRavenEmbedded()
    {
        var raven = RavenEmbeddedTestDatabase.Create();
        ExecuteTrackSearchIndex(raven.Store);
        return new TrackSearchTestEnvironment(
            new RavenTrackSearchIndex(raven.Store),
            results => SeedRaven(raven, results),
            raven);
    }

    private static void SeedRaven(RavenEmbeddedTestDatabase raven, params SearchResult[] results)
    {
        using var session = raven.Store.OpenSession();
        session.Advanced.WaitForIndexesAfterSaveChanges();

        foreach (var result in results)
        {
            var stableId = result.Isrc?.Value
                ?? result.Mbid?.Value
                ?? result.AppleId?.Value
                ?? result.SpotifyId?.Value
                ?? NormalizedSearchQuery.FromText($"{result.Title.Value} {result.Artist.Value}").Value;

            var document = Activator.CreateInstance(
                RavenTrackDocumentType,
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
                binder: null,
                args: null,
                culture: null)!;

            Set(document, "Id", $"track-catalogue/{stableId}");
            Set(document, "Title", result.Title.Value);
            Set(document, "Artist", result.Artist.Value);
            Set(document, "SearchText", NormalizedSearchQuery.FromText($"{result.Title.Value} {result.Artist.Value}").Value);
            Set(document, "Isrc", result.Isrc?.Value);
            Set(document, "Mbid", result.Mbid?.Value);
            Set(document, "AppleId", result.AppleId?.Value);
            Set(document, "SpotifyId", result.SpotifyId?.Value);
            Set(document, "DurationMs", null);

            session.Store(document);
        }

        session.SaveChanges();
    }

    private static readonly Type RavenTrackDocumentType = typeof(RavenTrackSearchIndex).Assembly
        .GetType("Soundtrail.Services.Api.Infrastructure.Raven.Documents.RavenTrackDocument", throwOnError: true)!;

    private static readonly Type TrackSearchIndexType = typeof(RavenTrackSearchIndex).Assembly
        .GetType("Soundtrail.Services.Api.Infrastructure.Raven.Indexes.TrackCatalogue_BySearchText", throwOnError: true)!;

    private static void Set(object target, string propertyName, object? value) =>
        RavenTrackDocumentType
            .GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
            .SetValue(target, value);

    private static void ExecuteTrackSearchIndex(IDocumentStore store)
    {
        var index = (AbstractIndexCreationTask)Activator.CreateInstance(
            TrackSearchIndexType,
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
            binder: null,
            args: null,
            culture: null)!;

        index.Execute(store);
    }
}