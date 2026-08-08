using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Discovery;
using Soundtrail.Services.Enrichment.Worker.Shared.MusicMetadata;

namespace Soundtrail.Services.Tests.Unit.Sociable.Features.GetAlbumsForArtist.Support;

internal sealed class ReadAlbumsByArtistIdPortFake : IReadAlbumsByArtistIdPort
{
    private readonly Dictionary<ArtistId, IReadOnlyList<CatalogDiscoveryEntry>> entries = [];

    public ReadAlbumsByArtistIdPortFake WithAlbums(ArtistId artistId, params CatalogDiscoveryEntry[] albums)
    {
        entries[artistId] = albums;
        return this;
    }

    public Task<IReadOnlyList<CatalogDiscoveryEntry>> ReadAsync(ArtistId artistId, CancellationToken cancellationToken) =>
        Task.FromResult(entries.GetValueOrDefault(artistId) ?? (IReadOnlyList<CatalogDiscoveryEntry>)[]);
}
