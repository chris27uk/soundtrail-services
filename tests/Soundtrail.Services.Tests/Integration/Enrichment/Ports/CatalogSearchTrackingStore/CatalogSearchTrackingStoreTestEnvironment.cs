using Raven.Client.Documents;
using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnCatalogSearchRequested.Adapters;
using Soundtrail.Services.Tests.Integration.Api.Infrastructure;
using Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

namespace Soundtrail.Services.Tests.Integration.Enrichment.Ports.CatalogSearchTrackingStore;

internal sealed class CatalogSearchTrackingStoreTestEnvironment : IDisposable
{
    private readonly RavenEmbeddedTestDatabase? raven;

    private CatalogSearchTrackingStoreTestEnvironment(
        ICatalogSearchTrackingStore store,
        RavenEmbeddedTestDatabase? raven)
    {
        Store = store;
        this.raven = raven;
    }

    public ICatalogSearchTrackingStore Store { get; }

    public static CatalogSearchTrackingStoreTestEnvironment Create(CatalogSearchTrackingStoreMode mode) =>
        mode switch
        {
            CatalogSearchTrackingStoreMode.InProcessFake => new CatalogSearchTrackingStoreTestEnvironment(new CatalogSearchTrackingStoreFake(), null),
            CatalogSearchTrackingStoreMode.RavenEmbedded => CreateRavenEmbedded(),
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
        };

    public void Dispose() => raven?.Dispose();

    private static CatalogSearchTrackingStoreTestEnvironment CreateRavenEmbedded()
    {
        var raven = RavenEmbeddedTestDatabase.Create();
        return new CatalogSearchTrackingStoreTestEnvironment(
            new RavenCatalogSearchTrackingStore(raven.Store),
            raven);
    }
}
