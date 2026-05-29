using Soundtrail.Services.EnrichmentWorker.Models;
using Soundtrail.Services.EnrichmentWorker.Ports;

namespace Soundtrail.Services.EnrichmentWorker.Infrastructure.BlobStorage;

public sealed class BlobMusicBrainzDatasetReader : IMusicBrainzDatasetReader
{
    public Task<TrackMapping?> TryResolveAsync(
        ResolutionDemand demand,
        CancellationToken cancellationToken) =>
        Task.FromResult<TrackMapping?>(null);
}
