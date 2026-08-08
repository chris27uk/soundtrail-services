using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Search;
using Soundtrail.Services.Enrichment.Worker.Shared.MusicMetadata;

namespace Soundtrail.Services.Tests.Unit.Sociable.Features.Search;

internal sealed class ReadCatalogEntriesBySearchCriteriaPortFake : IReadCatalogEntriesBySearchCriteriaPort
{
    private readonly Dictionary<string, IReadOnlyList<CatalogDiscoveryEntry>> entries = new(StringComparer.Ordinal);

    public ReadCatalogEntriesBySearchCriteriaPortFake WithEntries(
        SearchCriteria searchCriteria,
        params CatalogDiscoveryEntry[] catalogEntries)
    {
        entries[searchCriteria.NormalisedIdentifier] = catalogEntries;
        return this;
    }

    public Task<IReadOnlyList<CatalogDiscoveryEntry>> ReadAsync(
        SearchCriteria searchCriteria,
        CancellationToken cancellationToken) =>
        Task.FromResult(entries.GetValueOrDefault(searchCriteria.NormalisedIdentifier, []));
}
