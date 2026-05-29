using Soundtrail.Services.Enrichment.Features.AppleMusic;
using Soundtrail.Services.Enrichment.Features.LocalCache;
using Soundtrail.Services.Enrichment.Infrastructure.Orchestration;

namespace Soundtrail.Services.Enrichment.Worker.Infrastructure.AppleMusic;

public sealed class HttpITunesSearchClient : IITunesSearchClient
{
    public Task<TrackMapping?> TryResolveAsync(
        ResolutionDemand demand,
        CancellationToken cancellationToken) =>
        Task.FromResult<TrackMapping?>(null);
}
