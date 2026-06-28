using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Search;
using Raven.Client.Documents.Session;
using Soundtrail.Services.Tests.Integration.Api.Infrastructure;
using Soundtrail.Services.Tests.Support;
using Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

namespace Soundtrail.Services.Tests.Integration.Enrichment.Ports.CatalogSearchDiscoveryRepository;

internal sealed class CatalogSearchDiscoveryRepositoryTestEnvironment : IDisposable
{
    private readonly RavenEmbeddedTestDatabase? raven;
    private readonly IAsyncDocumentSession? session;

    private CatalogSearchDiscoveryRepositoryTestEnvironment(
        IEventStreamRepository<DiscoveryQueryKey, IDomainEvent> repository,
        RavenEmbeddedTestDatabase? raven,
        IAsyncDocumentSession? session = null)
    {
        Repository = repository;
        this.raven = raven;
        this.session = session;
    }

    public IEventStreamRepository<DiscoveryQueryKey, IDomainEvent> Repository { get; }

    public static CatalogSearchDiscoveryRepositoryTestEnvironment Create(CatalogSearchDiscoveryRepositoryMode mode) =>
        mode switch
        {
            CatalogSearchDiscoveryRepositoryMode.InProcessFake => new(new CatalogSearchDiscoveryRepositoryFake(), null),
            CatalogSearchDiscoveryRepositoryMode.RavenEmbedded => CreateRavenEmbedded(),
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
        };

    public void Dispose()
    {
        session?.Dispose();
        raven?.Dispose();
    }

    private static CatalogSearchDiscoveryRepositoryTestEnvironment CreateRavenEmbedded()
    {
        var raven = RavenEmbeddedTestDatabase.Create();
        var session = raven.Store.OpenAsyncSession();
        return new CatalogSearchDiscoveryRepositoryTestEnvironment(
            TestEventStreamRepositories.CreateDiscoveryQuery(session),
            raven,
            session);
    }
}
