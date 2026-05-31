using Soundtrail.Services.Enrichment.Features.Scheduling.Contracts;
using Soundtrail.Services.Enrichment.Features.Scheduling.Models;
using Soundtrail.Services.Features.Search.Models;

namespace Soundtrail.Services.Tests.Enrichment.Unit.Infrastructure
{
    internal sealed class FakeMusicCatalogSearch : IMusicCatalogSearch
    {
        private MusicCatalogId? catalogId = MusicCatalogId.From("mc_default");

        public void ResolveAs(MusicCatalogId musicCatalogId) => this.catalogId = musicCatalogId;

        public void Fails() => this.catalogId = null;

        public Task<MusicCatalogId?> SearchAsync(NormalizedSearchQuery query, CancellationToken cancellationToken) => Task.FromResult(this.catalogId);
    }
}
