using Soundtrail.Domain.Catalog.Albums;
using Soundtrail.Domain.Discovery;
using Soundtrail.Services.Enrichment.Worker.Shared.MusicMetadata;

namespace Soundtrail.Services.Tests.Unit.Sociable.Features.GetTracksForAlbum.Support;

internal sealed class ReadTracksByAlbumIdPortFake : IReadTracksByAlbumIdPort
{
    private readonly Dictionary<AlbumId, IReadOnlyList<CatalogDiscoveryEntry>> entries = [];

    public int ReadCallCount { get; private set; }

    public ReadTracksByAlbumIdPortFake WithTracks(AlbumId albumId, params CatalogDiscoveryEntry[] tracks)
    {
        entries[albumId] = tracks;
        return this;
    }

    public Task<IReadOnlyList<CatalogDiscoveryEntry>> ReadAsync(AlbumId albumId, CancellationToken cancellationToken)
    {
        ReadCallCount++;
        return Task.FromResult(entries.GetValueOrDefault(albumId) ?? (IReadOnlyList<CatalogDiscoveryEntry>)[]);
    }
}
