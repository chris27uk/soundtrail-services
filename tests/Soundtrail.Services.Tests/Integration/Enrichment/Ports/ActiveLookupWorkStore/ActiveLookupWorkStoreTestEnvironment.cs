using Soundtrail.Services.Enrichment.Orchestrator.Features.JustInTimeScheduling.Adapters;
using Soundtrail.Services.Enrichment.Orchestrator.Shared.Idempotency;
using Soundtrail.Services.Tests.Integration.Api.Infrastructure;
using Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

namespace Soundtrail.Services.Tests.Integration.Enrichment.Ports.ActiveLookupWorkStore;

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
