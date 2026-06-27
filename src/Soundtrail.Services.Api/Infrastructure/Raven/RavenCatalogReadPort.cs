using Raven.Client.Documents;
using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Browsing;
using Soundtrail.Services.Api.Infrastructure.Ports;
using Soundtrail.Services.Api.Infrastructure.Raven.Documents;

namespace Soundtrail.Services.Api.Infrastructure.Raven;

public sealed class RavenCatalogReadPort(IDocumentStore documentStore) : ICatalogReadPort
{
    public async Task<ArtistDetailsResponse?> GetArtistAsync(ArtistId artistId, CancellationToken cancellationToken)
    {
        using var session = documentStore.OpenAsyncSession();

        var artist = await session.LoadAsync<CatalogArtistRecordDto>(
            CatalogArtistRecordDto.GetDocumentId(artistId.Value),
            cancellationToken);

        if (artist is null)
        {
            return null;
        }

        var albums = await session.Query<CatalogAlbumRecordDto>()
            .Where(x => x.ArtistId == artistId.Value)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

        return new ArtistDetailsResponse(
            ArtistId.From(artist.ArtistId),
            artist.Name,
            albums.Select(ToAlbumSummary).ToArray());
    }

    public async Task<IReadOnlyList<TrackSummary>> ListTracksByArtistAsync(ArtistId artistId, CancellationToken cancellationToken)
    {
        using var session = documentStore.OpenAsyncSession();

        var tracks = await session.Query<CatalogTrackRecordDto>()
            .Where(x => x.ArtistId == artistId.Value)
            .OrderBy(x => x.AlbumName)
            .ThenBy(x => x.Title)
            .ToListAsync(cancellationToken);

        return tracks.Select(ToTrackSummary).ToArray();
    }

    public async Task<AlbumDetailsResponse?> GetAlbumAsync(ArtistId artistId, AlbumId albumId, CancellationToken cancellationToken)
    {
        using var session = documentStore.OpenAsyncSession();

        var album = await session.LoadAsync<CatalogAlbumRecordDto>(
            CatalogAlbumRecordDto.GetDocumentId(albumId.Value),
            cancellationToken);

        if (album is null || album.ArtistId != artistId.Value)
        {
            return null;
        }

        var tracks = await session.Query<CatalogTrackRecordDto>()
            .Where(x => x.AlbumId == albumId.Value && x.ArtistId == artistId.Value)
            .OrderBy(x => x.Title)
            .ToListAsync(cancellationToken);

        return new AlbumDetailsResponse(
            ArtistId.From(album.ArtistId),
            album.ArtistName,
            AlbumId.From(album.AlbumId),
            album.Name,
            album.ReleaseDate,
            tracks.Select(ToTrackSummary).ToArray());
    }

    public async Task<AlbumTracksResponse?> ListTracksByAlbumAsync(ArtistId artistId, AlbumId albumId, CancellationToken cancellationToken)
    {
        using var session = documentStore.OpenAsyncSession();

        var album = await session.LoadAsync<CatalogAlbumRecordDto>(
            CatalogAlbumRecordDto.GetDocumentId(albumId.Value),
            cancellationToken);

        if (album is null || album.ArtistId != artistId.Value)
        {
            return null;
        }

        var tracks = await session.Query<CatalogTrackRecordDto>()
            .Where(x => x.AlbumId == albumId.Value && x.ArtistId == artistId.Value)
            .OrderBy(x => x.Title)
            .ToListAsync(cancellationToken);

        return new AlbumTracksResponse(
            ArtistId.From(album.ArtistId),
            album.ArtistName,
            AlbumId.From(album.AlbumId),
            album.Name,
            tracks.Select(ToTrackSummary).ToArray());
    }

    public async Task<TrackDetailsResponse?> GetTrackAsync(ArtistId artistId, AlbumId albumId, TrackId trackId, CancellationToken cancellationToken)
    {
        using var session = documentStore.OpenAsyncSession();

        var track = await session.LoadAsync<CatalogTrackRecordDto>(
            CatalogTrackRecordDto.GetDocumentId(trackId.Value),
            cancellationToken);

        return track is null || track.ArtistId != artistId.Value || track.AlbumId != albumId.Value
            ? null
            : new TrackDetailsResponse(
                ArtistId.From(track.ArtistId),
                track.ArtistName,
                AlbumId.From(track.AlbumId),
                track.AlbumName,
                TrackId.From(track.TrackId),
                track.Title,
                track.Isrc,
                track.DurationMs,
                ResolvePlayabilityStatus(track.AvailableProviders, track.TerminallyUnavailableProviders),
                ToProviders(track.AvailableProviders),
                ToProviders(track.TerminallyUnavailableProviders),
                ToProviderReferences(track.ProviderReferences));
    }

    private static AlbumSummary ToAlbumSummary(CatalogAlbumRecordDto album) =>
        new(
            AlbumId.From(album.AlbumId),
            album.Name,
            album.ReleaseDate,
            ResolvePlayabilityStatus(album.AvailableProviders, album.TerminallyUnavailableProviders),
            ToProviders(album.AvailableProviders),
            ToProviders(album.TerminallyUnavailableProviders));

    private static TrackSummary ToTrackSummary(CatalogTrackRecordDto track) =>
        new(
            TrackId.From(track.TrackId),
            track.Title,
            AlbumId.From(track.AlbumId),
            track.AlbumName,
            track.Isrc,
            track.DurationMs,
            ResolvePlayabilityStatus(track.AvailableProviders, track.TerminallyUnavailableProviders),
            ToProviders(track.AvailableProviders),
            ToProviders(track.TerminallyUnavailableProviders),
            ToProviderReferences(track.ProviderReferences));

    private static IReadOnlyList<ProviderName> ToProviders(IEnumerable<string> values) =>
        values.Select(ProviderName.From).ToArray();

    private static IReadOnlyList<ProviderReference> ToProviderReferences(IEnumerable<CatalogProviderReferenceRecordDto>? values) =>
        (values ?? [])
        .Where(value => !string.IsNullOrWhiteSpace(value.ProviderId) && !string.IsNullOrWhiteSpace(value.Url))
        .Select(value => new ProviderReference(
            ProviderName.From(value.Provider),
            value.ProviderEntityType,
            value.ProviderId,
            new Uri(value.Url),
            value.DiscoveredAt))
        .ToArray();

    private static PlayabilityStatus ResolvePlayabilityStatus(
        IReadOnlyCollection<string> availableProviders,
        IReadOnlyCollection<string> terminalProviders)
    {
        if (availableProviders.Count > 0)
        {
            return PlayabilityStatus.Playable;
        }

        if (terminalProviders.Count > 0)
        {
            return PlayabilityStatus.TerminallyUnavailable;
        }

        return PlayabilityStatus.NotYetDiscovered;
    }
}
