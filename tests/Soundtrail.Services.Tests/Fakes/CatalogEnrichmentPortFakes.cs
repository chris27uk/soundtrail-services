using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Albums;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Catalog.Playlists;
using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Assesment;
using Soundtrail.Domain.Search;
using Soundtrail.Services.Enrichment.Orchestrator.Features.Prioritisation.OnMusicAssessmentRequired.Planning;
using Soundtrail.Services.Enrichment.Worker.Features.LookupPlaylistTracks.Ports;
using Soundtrail.Services.Enrichment.Worker.Shared.MusicMetadata;
using Soundtrail.Services.Enrichment.Worker.Shared.StreamingLocations;

namespace Soundtrail.Services.Tests.Fakes;

internal sealed class ReadPlaylistTracksByProviderPortFake : IReadPlaylistTracksByProviderPort
{
    private readonly Dictionary<(PlaylistId PlaylistId, ProviderName Provider), IReadOnlyList<TrackReference>> tracks = [];
    private readonly Exception? failure;

    private ReadPlaylistTracksByProviderPortFake(Exception? failure) => this.failure = failure;

    public ReadPlaylistTracksByProviderPortFake() : this(failure: null)
    {
    }

    public static ReadPlaylistTracksByProviderPortFake Empty() => new();

    public static ReadPlaylistTracksByProviderPortFake ThatThrows(Exception error) =>
        new(failure: error);

    public ReadPlaylistTracksByProviderPortFake WithTracks(
        PlaylistId playlistId,
        ProviderName provider,
        params TrackReference[] playlistTracks)
    {
        tracks[(playlistId, provider)] = playlistTracks;
        return this;
    }

    public Task<IReadOnlyList<TrackReference>> ReadAsync(
        PlaylistId playlistId,
        ProviderName provider,
        CancellationToken cancellationToken) =>
        failure is not null
            ? Task.FromException<IReadOnlyList<TrackReference>>(failure)
            : Task.FromResult(tracks.GetValueOrDefault((playlistId, provider), []));
}

internal sealed class ReadCatalogEntriesBySearchCriteriaPortFake : IReadCatalogEntriesBySearchCriteriaPort
{
    private readonly Dictionary<string, IReadOnlyList<CatalogDiscoveryEntry>> entries = new(StringComparer.Ordinal);

    public ReadCatalogEntriesBySearchCriteriaPortFake WithEntries(
        SearchCriteria searchCriteria,
        params CatalogDiscoveryEntry[] catalogEntries)
    {
        entries[searchCriteria.NormalisedIdentifier] = catalogEntries;
        return this;
    }

    public Task<IReadOnlyList<CatalogDiscoveryEntry>> ReadAsync(
        SearchCriteria searchCriteria,
        CancellationToken cancellationToken) =>
        Task.FromResult(entries.GetValueOrDefault(searchCriteria.NormalisedIdentifier, []));
}

internal sealed class ReadTrackForLookupPortFake : IReadTrackForLookupPort
{
    private readonly Dictionary<TrackId, TrackLookupContext> tracks = [];

    public ReadTrackForLookupPortFake WithTrack(TrackLookupContext track)
    {
        tracks[track.TrackId] = track;
        return this;
    }

    public Task<TrackLookupContext?> ReadAsync(TrackId trackId, CancellationToken cancellationToken) =>
        Task.FromResult(tracks.GetValueOrDefault(trackId));
}

internal sealed class ReadStreamingLocationByProviderPortFake : IReadStreamingLocationByProviderPort
{
    private readonly Dictionary<(string Isrc, ProviderName Provider), Uri> isrcLocations = [];
    private readonly Dictionary<(string ArtistName, string TrackTitle, ProviderName Provider), Uri> metadataLocations = [];

    public ReadStreamingLocationByProviderPortFake WithIsrcLocation(string isrc, ProviderName provider, Uri url)
    {
        isrcLocations[(isrc, provider)] = url;
        return this;
    }

    public ReadStreamingLocationByProviderPortFake WithMetadataLocation(
        string artistName,
        string trackTitle,
        ProviderName provider,
        Uri url)
    {
        metadataLocations[(artistName, trackTitle, provider)] = url;
        return this;
    }

    public Task<Uri?> ReadByIsrcAsync(string isrc, ProviderName provider, CancellationToken cancellationToken) =>
        Task.FromResult(isrcLocations.GetValueOrDefault((isrc, provider)));

    public Task<Uri?> ReadByTrackMetadataAsync(
        string artistName,
        string trackTitle,
        ProviderName provider,
        CancellationToken cancellationToken) =>
        Task.FromResult(metadataLocations.GetValueOrDefault((artistName, trackTitle, provider)));
}

internal sealed class ReadAlbumsByArtistIdPortFake : IReadAlbumsByArtistIdPort
{
    public Task<IReadOnlyList<CatalogDiscoveryEntry>> ReadAsync(ArtistId artistId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<CatalogDiscoveryEntry>>([]);
}

internal sealed class ReadTracksByArtistIdPortFake : IReadTracksByArtistIdPort
{
    private readonly Dictionary<ArtistId, IReadOnlyList<CatalogDiscoveryEntry>> entries = [];

    public ReadTracksByArtistIdPortFake WithTracks(ArtistId artistId, params CatalogDiscoveryEntry[] tracks)
    {
        entries[artistId] = tracks;
        return this;
    }

    public Task<IReadOnlyList<CatalogDiscoveryEntry>> ReadAsync(ArtistId artistId, CancellationToken cancellationToken) =>
        Task.FromResult(entries.GetValueOrDefault(artistId) ?? (IReadOnlyList<CatalogDiscoveryEntry>)[]);
}

internal sealed class ReadTracksByAlbumIdPortFake : IReadTracksByAlbumIdPort
{
    public Task<IReadOnlyList<CatalogDiscoveryEntry>> ReadAsync(AlbumId albumId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<CatalogDiscoveryEntry>>([]);
}

internal sealed class DiscoveryPlanningProjectionReaderFake : IDiscoveryPlanningProjectionReader
{
    private readonly Dictionary<string, DiscoveryPlanningProjection> projections = new(StringComparer.Ordinal);

    public DiscoveryPlanningProjectionReaderFake WithProjection(
        EnrichmentTarget target,
        DiscoveryPlanningProjection projection)
    {
        projections[target.NormalisedIdentifier] = projection;
        return this;
    }

    public Task<DiscoveryPlanningProjection> ReadAsync(EnrichmentTarget target, CancellationToken cancellationToken) =>
        Task.FromResult(projections.GetValueOrDefault(
            target.NormalisedIdentifier,
            new DiscoveryPlanningProjection(false, null, 0, 0)));
}
