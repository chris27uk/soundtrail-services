using Soundtrail.Services.Enrichment.Models;
using Soundtrail.Services.Enrichment.Ports;

namespace Soundtrail.Services.Enrichment.Worker.Infrastructure.AppleMusic;

public sealed class HttpITunesSearchClient : IITunesSearchClient
{
    public Task<TrackMapping?> TryResolveAsync(
        ResolutionDemand demand,
        CancellationToken cancellationToken) =>
        Task.FromResult<TrackMapping?>(null);
}
