using Raven.Client.Documents;
using Raven.Client.Documents.Indexes;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.BacklogScheduling.Adapters;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Persistence;
using Soundtrail.Services.Tests.Integration.Api.Infrastructure;
using Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;
using System.Reflection;

namespace Soundtrail.Services.Tests.Integration.Enrichment.Ports.PotentialCatalogLookupWorkStore;

internal sealed class PotentialCatalogLookupWorkStoreTestEnvironment : IDisposable
{
    private readonly RavenEmbeddedTestDatabase? raven;
    private readonly Action<PotentialCatalogLookupWork[]> seed;

    private PotentialCatalogLookupWorkStoreTestEnvironment(
        IPotentialCatalogLookupWorkStore store,
        Action<PotentialCatalogLookupWork[]> seed,
        RavenEmbeddedTestDatabase? raven)
    {
        Store = store;
        this.seed = seed;
        this.raven = raven;
    }

    public IPotentialCatalogLookupWorkStore Store { get; }

    public static PotentialCatalogLookupWorkStoreTestEnvironment Create(PotentialCatalogLookupWorkStorePortMode mode)
    {
        return mode switch
        {
            PotentialCatalogLookupWorkStorePortMode.InProcessFake => CreateFake(),
            PotentialCatalogLookupWorkStorePortMode.RavenEmbedded => CreateRavenEmbedded(),
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
        };
    }

    public void Seed(params PotentialCatalogLookupWork[] candidates) => this.seed(candidates);

    public void Dispose() => this.raven?.Dispose();

    private static PotentialCatalogLookupWorkStoreTestEnvironment CreateFake()
    {
        var fake = new PotentialCatalogLookupWorkStoreFake();
        return new PotentialCatalogLookupWorkStoreTestEnvironment(
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

    private static PotentialCatalogLookupWorkStoreTestEnvironment CreateRavenEmbedded()
    {
        var raven = RavenEmbeddedTestDatabase.Create();
        ExecutePlanningIndex(raven.Store);
        return new PotentialCatalogLookupWorkStoreTestEnvironment(
            new RavenPotentialCatalogLookupWorkStore(raven.Store),
            candidates => SeedRaven(raven, candidates),
            raven);
    }

    private static void SeedRaven(RavenEmbeddedTestDatabase raven, params PotentialCatalogLookupWork[] candidates)
    {
        using var session = raven.Store.OpenSession();
        session.Advanced.WaitForIndexesAfterSaveChanges();

        foreach (var candidate in candidates)
        {
            var document = Activator.CreateInstance(
                RavenPotentialCatalogLookupWorkRecordDtoType,
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
                binder: null,
                args: null,
                culture: null)!;

            Set(document, "Id", $"potential-catalog-lookup-work/{Uri.EscapeDataString(candidate.MusicCatalogId.Value)}");
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

    private static readonly Type RavenPotentialCatalogLookupWorkRecordDtoType = typeof(RavenPotentialCatalogLookupWorkStore).Assembly
        .GetType("Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.BacklogScheduling.Adapters.Documents.RavenPotentialCatalogLookupWorkRecordDto", throwOnError: true)!;

    private static readonly Type PotentialCatalogLookupWorksByPlanningIndexType = typeof(RavenPotentialCatalogLookupWorkStore).Assembly
        .GetType("Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.BacklogScheduling.Adapters.Indexes.PotentialCatalogLookupWork_ByPlanning", throwOnError: true)!;

    private static void ExecutePlanningIndex(IDocumentStore store)
    {
        var index = (AbstractIndexCreationTask)Activator.CreateInstance(
            PotentialCatalogLookupWorksByPlanningIndexType,
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
            binder: null,
            args: null,
            culture: null)!;

        index.Execute(store);
    }

    private static void Set(object target, string propertyName, object? value) =>
        RavenPotentialCatalogLookupWorkRecordDtoType
            .GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
            .SetValue(target, value);
}
