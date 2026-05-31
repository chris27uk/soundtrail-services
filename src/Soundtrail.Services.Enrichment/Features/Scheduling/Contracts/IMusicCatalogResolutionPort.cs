using Soundtrail.Services.Enrichment.Features.Scheduling.Models;

namespace Soundtrail.Services.Enrichment.Features.Scheduling.Contracts;

public interface IMusicCatalogResolutionPort
{
    Task<MusicCatalogId?> ResolveAsync(LookupMusicRequest request, CancellationToken cancellationToken);
}
