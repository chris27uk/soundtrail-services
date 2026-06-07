using Soundtrail.Services.Enrichment.DiscoveryPlanner.Infrastructure.Raven;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Idempotency;
using Soundtrail.Services.Tests.Api.Integration.Infrastructure;
using Soundtrail.Services.Tests.Enrichment.Unit.Infrastructure;

namespace Soundtrail.Services.Tests.Enrichment.Integration.Ports.ActiveLookupWorkStore;

internal sealed class ActiveLookupWorkStoreTestEnvironment : IDisposable
{
    private readonly RavenEmbeddedTestDatabase? raven;

    private ActiveLookupWorkStoreTestEnvironment(
        IActiveLookupWorkStore store,
        RavenEmbeddedTestDatabase? raven)
    {
        Store = store;
        this.raven = raven;
    }

    public IActiveLookupWorkStore Store { get; }

    public static ActiveLookupWorkStoreTestEnvironment Create(ActiveLookupWorkStorePortMode mode)
    {
        return mode switch
        {
            ActiveLookupWorkStorePortMode.InProcessFake => new ActiveLookupWorkStoreTestEnvironment(new ActiveLookupWorkStoreFake(), raven: null),
            ActiveLookupWorkStorePortMode.RavenEmbedded => CreateRavenEmbedded(),
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
        };
    }

    public void Dispose() => this.raven?.Dispose();

    private static ActiveLookupWorkStoreTestEnvironment CreateRavenEmbedded()
    {
        var raven = RavenEmbeddedTestDatabase.Create();
        return new ActiveLookupWorkStoreTestEnvironment(
            new RavenActiveLookupWorkStore(raven.Store),
            raven);
    }
}
