using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Discovery;
using Soundtrail.Services.Enrichment.Worker.Shared.MusicMetadata;

namespace Soundtrail.Services.Tests.Unit.Sociable.Features.GetTracksForArtist.Support;

internal sealed class ReadTracksByArtistIdPortFake : IReadTracksByArtistIdPort
{
    private readonly Dictionary<ArtistId, IReadOnlyList<CatalogDiscoveryEntry>> entries = [];

    public int ReadCallCount { get; private set; }

    public ReadTracksByArtistIdPortFake WithTracks(ArtistId artistId, params CatalogDiscoveryEntry[] tracks)
    {
        entries[artistId] = tracks;
        return this;
    }

    public Task<IReadOnlyList<CatalogDiscoveryEntry>> ReadAsync(ArtistId artistId, CancellationToken cancellationToken)
    {
        ReadCallCount++;
        return Task.FromResult(entries.GetValueOrDefault(artistId) ?? (IReadOnlyList<CatalogDiscoveryEntry>)[]);
    }
}
