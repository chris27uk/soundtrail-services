using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Events;
using Soundtrail.Domain.Search;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogCandidateIdentified;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogCandidateIdentified.Support;
using Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

namespace Soundtrail.Services.Tests.Unit.InternalProjector.Infrastructure;

internal sealed class CatalogCandidateIdentifiedHandlerTestEnvironment
{
    private CatalogCandidateIdentifiedHandlerTestEnvironment()
    {
        WorkRepository = new CatalogDiscoveryWorkRepositoryFake();
        Bus = new CommandBusFake();
        Handler = new CatalogCandidateIdentifiedHandler(
            WorkRepository,
            Bus);
    }

    public CatalogCandidateIdentifiedHandler Handler { get; }

    public CatalogDiscoveryWorkRepositoryFake WorkRepository { get; }

    public CommandBusFake Bus { get; }

    public static CatalogCandidateIdentifiedHandlerTestEnvironment Create() => new();

    public CatalogCandidateIdentifiedCommand Command(
        MusicSearchCriteria searchCriteria,
        MusicCatalogId musicCatalogId,
        int version = 1) =>
        new(
            searchCriteria,
            [
                new VersionedCatalogSearchDiscoveryEvent(
                    version,
                    new CatalogCandidateIdentified(
                        searchCriteria,
                        musicCatalogId,
                        1,
                        10,
                        Clock,
                        CorrelationId.From("corr-1")))
            ]);

    private static readonly DateTimeOffset Clock = new(2026, 6, 28, 12, 0, 0, TimeSpan.Zero);
}
