using Soundtrail.Services.Enrichment.Models;
using Soundtrail.Services.Enrichment.Ports;

namespace Soundtrail.Services.Enrichment.Worker.Infrastructure.MusicBrainz;

public sealed class BlobMusicBrainzDatasetReader : IMusicBrainzDatasetReader
{
    public Task<TrackMapping?> TryResolveAsync(
        ResolutionDemand demand,
        CancellationToken cancellationToken) =>
        Task.FromResult<TrackMapping?>(null);
}
