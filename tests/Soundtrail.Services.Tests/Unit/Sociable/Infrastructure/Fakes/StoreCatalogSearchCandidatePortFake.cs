using Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchCandidateChanged;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchCandidateChanged.Adapters;

namespace Soundtrail.Services.Tests.Unit.Sociable.Infrastructure.Fakes
{
    internal sealed class StoreCatalogSearchCandidatePortFake : IStoreCatalogSearchCandidatePort
    {
        private readonly Dictionary<string, CatalogSearchCandidateProjection> candidates = new(StringComparer.Ordinal);

        public IReadOnlyCollection<CatalogSearchCandidateProjection> Candidates => this.candidates.Values;

        public Task StoreAsync(CatalogSearchCandidateProjection projection, CancellationToken cancellationToken)
        {
            this.candidates[projection.CatalogItemId] = projection;
            return Task.CompletedTask;
        }
    }
}
