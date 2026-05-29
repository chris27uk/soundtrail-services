using Soundtrail.Services.Enrichment.Features.LocalCache;
using Soundtrail.Services.Enrichment.Features.MusicBrainz;
using Soundtrail.Services.Enrichment.Infrastructure.Orchestration;

namespace Soundtrail.Services.Enrichment.Worker.Infrastructure.MusicBrainz;

public sealed class BlobMusicBrainzDatasetReader : IMusicBrainzDatasetReader
{
    public Task<TrackMapping?> TryResolveAsync(
        ResolutionDemand demand,
        CancellationToken cancellationToken) =>
        Task.FromResult<TrackMapping?>(null);
}
