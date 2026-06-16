using Soundtrail.Domain.Discovery;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.JustInTimeScheduling.Adapters;
using Soundtrail.Services.Tests.Integration.Api.Infrastructure;
using Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

namespace Soundtrail.Services.Tests.Integration.Enrichment.Ports.CatalogSearchDiscoveryRepository;

internal sealed class CatalogSearchDiscoveryRepositoryTestEnvironment : IDisposable
{
    private readonly RavenEmbeddedTestDatabase? raven;

    private CatalogSearchDiscoveryRepositoryTestEnvironment(
        ICatalogSearchDiscoveryRepository repository,
        RavenEmbeddedTestDatabase? raven)
    {
        Repository = repository;
        this.raven = raven;
    }

    public ICatalogSearchDiscoveryRepository Repository { get; }

    public static CatalogSearchDiscoveryRepositoryTestEnvironment Create(CatalogSearchDiscoveryRepositoryMode mode) =>
        mode switch
        {
            CatalogSearchDiscoveryRepositoryMode.InProcessFake => new(new CatalogSearchDiscoveryRepositoryFake(), null),
            CatalogSearchDiscoveryRepositoryMode.RavenEmbedded => CreateRavenEmbedded(),
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
        };

    public void Dispose() => raven?.Dispose();

    private static CatalogSearchDiscoveryRepositoryTestEnvironment CreateRavenEmbedded()
    {
        var raven = RavenEmbeddedTestDatabase.Create();
        return new CatalogSearchDiscoveryRepositoryTestEnvironment(
            new RavenCatalogSearchDiscoveryRepository(raven.Store),
            raven);
    }
}
