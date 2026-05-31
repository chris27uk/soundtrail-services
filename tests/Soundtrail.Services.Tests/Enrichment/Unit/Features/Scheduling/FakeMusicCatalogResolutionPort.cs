using Soundtrail.Services.Enrichment.Features.Scheduling.Contracts;
using Soundtrail.Services.Enrichment.Features.Scheduling.Models;

namespace Soundtrail.Services.Tests.Enrichment.Unit.Features.Scheduling
{
    internal sealed class FakeMusicCatalogResolutionPort : IMusicCatalogResolutionPort
    {
        private MusicCatalogId? catalogId = MusicCatalogId.From("mc_default");

        public Task<MusicCatalogId?> ResolveAsync(LookupMusicRequest request, CancellationToken cancellationToken) => Task.FromResult(this.catalogId);

        public void ResolveAs(MusicCatalogId musicCatalogId) => this.catalogId = musicCatalogId;

        public void Fails() => this.catalogId = null;
    }
}
