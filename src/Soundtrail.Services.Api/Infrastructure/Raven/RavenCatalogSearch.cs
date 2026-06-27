using Raven.Client.Documents;
using Soundtrail.Contracts;
using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Search;
using Soundtrail.Services.Api.Features.SearchCatalog.Ports;
using Soundtrail.Services.Api.Infrastructure.Raven.Documents;
using Soundtrail.Services.Api.Infrastructure.Raven.Indexes;
using Soundtrail.Translators.Discovery;

namespace Soundtrail.Services.Api.Infrastructure.Raven;

public sealed class RavenCatalogSearch(IDocumentStore documentStore) : ICatalogSearchPort
{
    public async Task<LocalCatalogSearchResponse> SearchAsync(
        SearchCatalogCommand command,
        CancellationToken cancellationToken)
    {
        using var session = documentStore.OpenAsyncSession();
        var take = command.Offset.Value + command.Limit.Value;

        var results = new List<SearchCatalogResult>();

        if (command.Types.Includes(SearchResultType.Artist))
        {
            var artists = await session
                .Query<CatalogArtistRecordDto, Search_Artists>()
                .Search(x => x.SearchText, command.Query)
                .Take(take)
                .ToListAsync(cancellationToken);

            results.AddRange(artists
                .Where(artist => command.Playback.AllowsAny(ToProviders(artist.AvailableProviders)))
                .Select(artist => new SearchCatalogResult(
                    SearchResultType.Artist,
                    artist.ArtistId,
                    artist.Name,
                    artist.ArtistId,
                    artist.Name,
                    AlbumId: null,
                    AlbumName: null,
                    ResolvePlayabilityStatus(artist.AvailableProviders, artist.TerminallyUnavailableProviders),
                    ToProviders(artist.AvailableProviders),
                    ToProviders(artist.TerminallyUnavailableProviders),
                    [])));
        }

        if (command.Types.Includes(SearchResultType.Album))
        {
            var albums = await session
                .Query<CatalogAlbumRecordDto, Search_Albums>()
                .Search(x => x.SearchText, command.Query)
                .Take(take)
                .ToListAsync(cancellationToken);

            results.AddRange(albums
                .Where(album => command.Playback.AllowsAny(ToProviders(album.AvailableProviders)))
                .Select(album => new SearchCatalogResult(
                    SearchResultType.Album,
                    album.AlbumId,
                    album.Name,
                    album.ArtistId,
                    album.ArtistName,
                    album.AlbumId,
                    album.Name,
                    ResolvePlayabilityStatus(album.AvailableProviders, album.TerminallyUnavailableProviders),
                    ToProviders(album.AvailableProviders),
                    ToProviders(album.TerminallyUnavailableProviders),
                    [])));
        }

        if (command.Types.Includes(SearchResultType.Track))
        {
            var tracks = await session
                .Query<CatalogTrackRecordDto, Search_Tracks>()
                .Search(x => x.SearchText, command.Query)
                .Take(take)
                .ToListAsync(cancellationToken);

            results.AddRange(tracks
                .Where(track => command.Playback.AllowsAny(ToProviders(track.AvailableProviders)))
                .Select(track => new SearchCatalogResult(
                    SearchResultType.Track,
                    track.TrackId,
                    track.Title,
                    track.ArtistId,
                    track.ArtistName,
                    track.AlbumId,
                    track.AlbumName,
                    ResolvePlayabilityStatus(track.AvailableProviders, track.TerminallyUnavailableProviders),
                    ToProviders(track.AvailableProviders),
                    ToProviders(track.TerminallyUnavailableProviders),
                    ToProviderReferences(track.ProviderReferences))));
        }

        var pagedResults = results
            .Skip(command.Offset.Value)
            .Take(command.Limit.Value)
            .ToArray();

        var discoveryStatus = await session.LoadAsync<CatalogSearchStatusRecordDto>(
            CatalogSearchStatusRecordDto.GetDocumentId(
                MusicSearchTermPersistentIdTranslator.ToPersistentId(command.ToMusicSearchTerm())),
            cancellationToken);

        return new LocalCatalogSearchResponse(
            pagedResults,
            discoveryStatus is null
                ? null
                : new SearchDiscovery(
                    discoveryStatus.WillBeLookedUp,
                    discoveryStatus.Reason,
                    discoveryStatus.EstimatedRetryAfterSeconds),
            IsComplete: pagedResults.Length > 0 && pagedResults.All(CatalogSearchCompletenessEvaluator.IsComplete));
    }

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
