using Soundtrail.Services.Enrichment.Features.JustInTimeScheduling.Idempotency;
using Soundtrail.Services.Enrichment.Worker.Infrastructure.Raven;
using Soundtrail.Services.Tests.Enrichment.Unit.Infrastructure;
using Soundtrail.Services.Tests.Api.Integration.Infrastructure;

namespace Soundtrail.Services.Tests.Enrichment.Integration.Ports.ActiveLookupWorkStore.Contract;

public enum ActiveLookupWorkStorePortMode
{
    InProcessFake,
    RavenEmbedded
}

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

    public void Dispose() => raven?.Dispose();

    private static ActiveLookupWorkStoreTestEnvironment CreateRavenEmbedded()
    {
        var raven = RavenEmbeddedTestDatabase.Create();
        return new ActiveLookupWorkStoreTestEnvironment(
            new RavenActiveLookupWorkStore(raven.Store),
            raven);
    }
}
