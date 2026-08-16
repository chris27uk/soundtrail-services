using Soundtrail.Domain.Catalog.Albums;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Discovery;
using Soundtrail.Adapters.MusicBrainzDumpFreshness;

namespace Soundtrail.Services.Tests.Unit.Sociable.Infrastructure.Fakes;

internal sealed class MusicBrainzDumpFreshnessEvaluatorFake : IMusicBrainzDumpFreshnessEvaluator
{
    private MusicBrainzDumpFreshnessDecision artistAlbumsDecision =
        MusicBrainzDumpFreshnessDecision.NeedsLiveLookup();
    private MusicBrainzDumpFreshnessDecision artistTracksDecision =
        MusicBrainzDumpFreshnessDecision.NeedsLiveLookup();
    private MusicBrainzDumpFreshnessDecision albumTracksDecision =
        MusicBrainzDumpFreshnessDecision.NeedsLiveLookup();

    public MusicBrainzDumpFreshnessEvaluatorFake WithArtistAlbums(
        MusicBrainzDumpFreshnessDecision decision)
    {
        artistAlbumsDecision = decision;
        return this;
    }

    public MusicBrainzDumpFreshnessEvaluatorFake WithArtistTracks(
        MusicBrainzDumpFreshnessDecision decision)
    {
        artistTracksDecision = decision;
        return this;
    }

    public MusicBrainzDumpFreshnessEvaluatorFake WithAlbumTracks(
        MusicBrainzDumpFreshnessDecision decision)
    {
        albumTracksDecision = decision;
        return this;
    }

    public MusicBrainzDumpFreshnessEvaluatorFake WithArtistAlbumsCatalog(
        params CatalogDiscoveryEntry[] entries) =>
        WithArtistAlbums(MusicBrainzDumpFreshnessDecision.UseExistingCatalog(entries));

    public MusicBrainzDumpFreshnessEvaluatorFake WithArtistTracksCatalog(
        params CatalogDiscoveryEntry[] entries) =>
        WithArtistTracks(MusicBrainzDumpFreshnessDecision.UseExistingCatalog(entries));

    public MusicBrainzDumpFreshnessEvaluatorFake WithAlbumTracksCatalog(
        params CatalogDiscoveryEntry[] entries) =>
        WithAlbumTracks(MusicBrainzDumpFreshnessDecision.UseExistingCatalog(entries));

    public Task<MusicBrainzDumpFreshnessDecision> EvaluateArtistAlbumsAsync(
        ArtistId artistId,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(artistAlbumsDecision);

    public Task<MusicBrainzDumpFreshnessDecision> EvaluateArtistTracksAsync(
        ArtistId artistId,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(artistTracksDecision);

    public Task<MusicBrainzDumpFreshnessDecision> EvaluateAlbumTracksAsync(
        AlbumId albumId,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(albumTracksDecision);
}
