using System.Collections.Concurrent;
using Soundtrail.Contracts.Common;
using Soundtrail.Services.Internal.Projector.Features.ProjectMusicTrackProjection.Ports;
using Soundtrail.Domain.Model;

namespace Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

public sealed class MusicTrackProjectionStoreFake : ILoadMusicTrackProjectionPort, ISaveMusicTrackProjectionPort
{
    private readonly ConcurrentDictionary<string, MusicTrackProjection> projections = [];

    public IReadOnlyDictionary<string, MusicTrackProjection> Projections => projections;

    public Task<MusicTrackProjection> LoadAsync(
        MusicCatalogId musicCatalogId,
        CancellationToken cancellationToken)
    {
        projections.TryGetValue(musicCatalogId.Value, out var projection);
        return Task.FromResult(projection ?? new MusicTrackProjection());
    }

    public Task SaveAsync(
        MusicCatalogId musicCatalogId,
        MusicTrackProjection projection,
        CancellationToken cancellationToken)
    {
        projections[musicCatalogId.Value] = projection;
        return Task.CompletedTask;
    }
}
