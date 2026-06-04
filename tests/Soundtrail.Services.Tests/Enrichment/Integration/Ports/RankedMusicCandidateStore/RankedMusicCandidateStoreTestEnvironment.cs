using Raven.Client.Documents;
using Raven.Client.Documents.Indexes;
using Soundtrail.Services.Enrichment.Shared.Persistence;
using Soundtrail.Services.Enrichment.Shared.Search;
using Soundtrail.Services.Enrichment.Worker.Infrastructure.Raven;
using Soundtrail.Services.Tests.Api.Integration.Infrastructure;
using System.Reflection;

namespace Soundtrail.Services.Tests.Enrichment.Integration.Ports.RankedMusicCandidateStore.RavenEmbedded;

internal sealed class RankedMusicCandidateStoreTestEnvironment : IDisposable
{
    private readonly RavenEmbeddedTestDatabase raven;

    private RankedMusicCandidateStoreTestEnvironment(
        IRankedMusicCandidateStore store,
        RavenEmbeddedTestDatabase raven)
    {
        Store = store;
        this.raven = raven;
    }

    public IRankedMusicCandidateStore Store { get; }

    public static RankedMusicCandidateStoreTestEnvironment Create()
    {
        var raven = RavenEmbeddedTestDatabase.Create();
        ExecutePlanningIndex(raven.Store);
        return new RankedMusicCandidateStoreTestEnvironment(
            new RavenRankedMusicCandidateStore(raven.Store),
            raven);
    }

    public void Seed(params RankedMusicCandidate[] candidates)
    {
        using var session = raven.Store.OpenSession();
        session.Advanced.WaitForIndexesAfterSaveChanges();

        foreach (var candidate in candidates)
        {
            var document = Activator.CreateInstance(
                RavenRankedMusicCandidateDocumentType,
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
                binder: null,
                args: null,
                culture: null)!;

            Set(document, "Id", $"ranked-music-candidates/{Uri.EscapeDataString(candidate.MusicCatalogId.Value)}");
            Set(document, "MusicCatalogId", candidate.MusicCatalogId.Value);
            Set(document, "RequestCount", candidate.RequestCount);
            Set(document, "HighestTrustLevelSeen", candidate.HighestTrustLevelSeen);
            Set(document, "RiskScore", candidate.RiskScore);
            Set(document, "Status", candidate.Status.ToString());
            Set(document, "NextEligibleAt", candidate.NextEligibleAt);

            session.Store(document);
        }

        session.SaveChanges();
    }

    public void Dispose() => raven.Dispose();

    private static readonly Type RavenRankedMusicCandidateDocumentType = typeof(RavenRankedMusicCandidateStore).Assembly
        .GetType("Soundtrail.Services.Enrichment.Worker.Infrastructure.Raven.Documents.RavenRankedMusicCandidateDocument", throwOnError: true)!;

    private static readonly Type RankedMusicCandidatesByPlanningIndexType = typeof(RavenRankedMusicCandidateStore).Assembly
        .GetType("Soundtrail.Services.Enrichment.Worker.Infrastructure.Raven.Indexes.RankedMusicCandidates_ByPlanning", throwOnError: true)!;

    private static void ExecutePlanningIndex(IDocumentStore store)
    {
        var index = (AbstractIndexCreationTask)Activator.CreateInstance(
            RankedMusicCandidatesByPlanningIndexType,
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
            binder: null,
            args: null,
            culture: null)!;

        index.Execute(store);
    }

    private static void Set(object target, string propertyName, object? value) =>
        RavenRankedMusicCandidateDocumentType
            .GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
            .SetValue(target, value);
}
