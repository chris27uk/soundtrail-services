using Raven.Client.Documents;
using Raven.Client.Documents.Indexes;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.BacklogScheduling.Adapters;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Persistence;
using Soundtrail.Services.Tests.Integration.Api.Infrastructure;
using Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;
using System.Reflection;

namespace Soundtrail.Services.Tests.Integration.Enrichment.Ports.RankedMusicCandidateStore;

internal sealed class RankedMusicCandidateStoreTestEnvironment : IDisposable
{
    private readonly RavenEmbeddedTestDatabase? raven;
    private readonly Action<RankedMusicCandidate[]> seed;

    private RankedMusicCandidateStoreTestEnvironment(
        IRankedMusicCandidateStore store,
        Action<RankedMusicCandidate[]> seed,
        RavenEmbeddedTestDatabase? raven)
    {
        Store = store;
        this.seed = seed;
        this.raven = raven;
    }

    public IRankedMusicCandidateStore Store { get; }

    public static RankedMusicCandidateStoreTestEnvironment Create(RankedMusicCandidateStorePortMode mode)
    {
        return mode switch
        {
            RankedMusicCandidateStorePortMode.InProcessFake => CreateFake(),
            RankedMusicCandidateStorePortMode.RavenEmbedded => CreateRavenEmbedded(),
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
        };
    }

    public void Seed(params RankedMusicCandidate[] candidates) => this.seed(candidates);

    public void Dispose() => this.raven?.Dispose();

    private static RankedMusicCandidateStoreTestEnvironment CreateFake()
    {
        var fake = new RankedMusicCandidateStoreFake();
        return new RankedMusicCandidateStoreTestEnvironment(
            fake,
            candidates =>
            {
                foreach (var candidate in candidates)
                {
                    fake.Seed(candidate);
                }
            },
            raven: null);
    }

    private static RankedMusicCandidateStoreTestEnvironment CreateRavenEmbedded()
    {
        var raven = RavenEmbeddedTestDatabase.Create();
        ExecutePlanningIndex(raven.Store);
        return new RankedMusicCandidateStoreTestEnvironment(
            new RavenRankedMusicCandidateStore(raven.Store),
            candidates => SeedRaven(raven, candidates),
            raven);
    }

    private static void SeedRaven(RavenEmbeddedTestDatabase raven, params RankedMusicCandidate[] candidates)
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

    private static readonly Type RavenRankedMusicCandidateDocumentType = typeof(RavenRankedMusicCandidateStore).Assembly
        .GetType("Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.BacklogScheduling.Adapters.Documents.RavenRankedMusicCandidateDocument", throwOnError: true)!;

    private static readonly Type RankedMusicCandidatesByPlanningIndexType = typeof(RavenRankedMusicCandidateStore).Assembly
        .GetType("Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.BacklogScheduling.Adapters.Indexes.RankedMusicCandidates_ByPlanning", throwOnError: true)!;

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
