using Soundtrail.Services.Enrichment.Features.JustInTimeScheduling.Idempotency;
using Soundtrail.Services.Enrichment.Worker.Infrastructure.Raven;
using Soundtrail.Services.Tests.Api.Integration.Infrastructure;

namespace Soundtrail.Services.Tests.Enrichment.Integration.Ports.ActiveLookupWorkStore.RavenEmbedded;

internal sealed class ActiveLookupWorkStoreTestEnvironment : IDisposable
{
    private readonly RavenEmbeddedTestDatabase raven;

    private ActiveLookupWorkStoreTestEnvironment(
        IActiveLookupWorkStore store,
        RavenEmbeddedTestDatabase raven)
    {
        Store = store;
        this.raven = raven;
    }

    public IActiveLookupWorkStore Store { get; }

    public static ActiveLookupWorkStoreTestEnvironment Create()
    {
        var raven = RavenEmbeddedTestDatabase.Create();
        return new ActiveLookupWorkStoreTestEnvironment(
            new RavenActiveLookupWorkStore(raven.Store),
            raven);
    }

    public void Dispose() => raven.Dispose();
}
