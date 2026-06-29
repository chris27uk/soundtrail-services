using Soundtrail.Domain.Catalog;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnKnownArtistRequested.Ports;

public interface ILoadKnownCatalogArtistPort
{
    Task<KnownCatalogArtistLookupData?> LoadAsync(ArtistId artistId, CancellationToken cancellationToken);
}
