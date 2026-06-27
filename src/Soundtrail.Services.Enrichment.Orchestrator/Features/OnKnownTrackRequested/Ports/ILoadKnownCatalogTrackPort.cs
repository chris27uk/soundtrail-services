using Soundtrail.Domain.Catalog;
using Soundtrail.Services.Enrichment.Orchestrator.Shared.Search;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnKnownTrackRequested.Ports;

public interface ILoadKnownCatalogTrackPort
{
    Task<LocalMusicTrackSearchResult?> LoadAsync(TrackId trackId, CancellationToken cancellationToken);
}
