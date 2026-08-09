using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Albums;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Catalog.Playlists;
using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Aggregates;
using Soundtrail.Domain.Discovery.Messages;
using Soundtrail.Domain.Search;

namespace Soundtrail.Adapters.TypeRegistry.Registrations;

public sealed class ExternalTransportTranslationRegistration : ITypeTranslationRegistration
{
    public void Register(TypeTranslationRegistry registry)
    {
        registry.RegisterPair<AssessWorkMessage, AssessMusicCatalogItemCommandDto>(
            toDto: message => new AssessMusicCatalogItemCommandDto(
                message.Id.Value,
                message.CorrelationId.Value,
                message.CreatedAt,
                ToDto(message.Priority),
                GetTargetItemKind(message.Target),
                GetTargetItemValue(message.Target),
                GetTargetResourceKind(message.Target),
                GetTargetResourceValue(message.Target),
                GetTargetResourceItemKind(message.Target),
                GetSearchTypesOrNull(message.Target),
                message.TrustLevel,
                message.RiskScore),
            toDomainObject: dto => new AssessWorkMessage(
                MessageId.From(dto.CommandId),
                CorrelationId.From(dto.CorrelationId),
                dto.CreatedAt,
                ParseTarget(
                    dto.ItemKindDto,
                    dto.ItemValue,
                    dto.ResourceKindDto,
                    dto.ResourceValue,
                    dto.ResourceItemKind,
                    dto.SearchTypes),
                ParsePriority(dto.Priority),
                dto.TrustLevel,
                dto.RiskScore));

        registry.RegisterPair<DispatchLookupWork, DispatchLookupWorkCommandDto>(
            toDto: message => new DispatchLookupWorkCommandDto(
                message.Id.Value,
                message.CorrelationId.Value,
                message.CreatedAt,
                ToDto(message.Priority),
                GetTargetKind(message.Target),
                GetTargetValue(message.Target),
                GetTargetItemKindOrNull(message.Target),
                GetSearchTypes(message.Target)),
            toDomainObject: dto => new DispatchLookupWork(
                ParseTarget(dto.TargetKind, dto.TargetValue, dto.TargetItemKind, dto.SearchTypes),
                ParsePriority(dto.Priority),
                MessageId.From(dto.CommandId),
                CorrelationId.From(dto.CorrelationId),
                dto.CreatedAt));

        registry.RegisterPair<LookupMusicbrainzSearchResultsMessage, MusicBrainzLookupCommandDto>(
            toDto: message => new MusicBrainzLookupCommandDto(
                message.Id.Value,
                message.CorrelationId.Value,
                message.CreatedAt,
                ToDto(message.Priority),
                "search",
                message.SearchCriteria.Query,
                (int)message.SearchCriteria.SearchTypes,
                null,
                null),
            toDomainObject: dto => new LookupMusicbrainzSearchResultsMessage(
                MessageId.From(dto.CommandId),
                CorrelationId.From(dto.CorrelationId),
                dto.CreatedAt,
                ParsePriority(dto.Priority),
                new SearchCriteria(dto.Query ?? string.Empty, (SearchType)dto.SearchTypes)));

        registry.RegisterPair<LookupMusicbrainzArtistAlbumsMessage, MusicBrainzLookupCommandDto>(
            toDto: message => new MusicBrainzLookupCommandDto(
                message.Id.Value,
                message.CorrelationId.Value,
                message.CreatedAt,
                ToDto(message.Priority),
                "artist-albums",
                null,
                0,
                message.ArtistId.Value,
                null),
            toDomainObject: dto => new LookupMusicbrainzArtistAlbumsMessage(
                MessageId.From(dto.CommandId),
                CorrelationId.From(dto.CorrelationId),
                dto.CreatedAt,
                ParsePriority(dto.Priority),
                ArtistId.From(dto.ArtistId ?? throw new InvalidOperationException("Artist id is required."))));

        registry.RegisterPair<LookupMusicbrainzArtistTracksMessage, MusicBrainzLookupCommandDto>(
            toDto: message => new MusicBrainzLookupCommandDto(
                message.Id.Value,
                message.CorrelationId.Value,
                message.CreatedAt,
                ToDto(message.Priority),
                "artist-tracks",
                null,
                0,
                message.ArtistId.Value,
                null),
            toDomainObject: dto => new LookupMusicbrainzArtistTracksMessage(
                MessageId.From(dto.CommandId),
                CorrelationId.From(dto.CorrelationId),
                dto.CreatedAt,
                ParsePriority(dto.Priority),
                ArtistId.From(dto.ArtistId ?? throw new InvalidOperationException("Artist id is required."))));

        registry.RegisterPair<LookupMusicbrainzAlbumTracksMessage, MusicBrainzLookupCommandDto>(
            toDto: message => new MusicBrainzLookupCommandDto(
                message.Id.Value,
                message.CorrelationId.Value,
                message.CreatedAt,
                ToDto(message.Priority),
                "album-tracks",
                null,
                0,
                null,
                message.AlbumId.StableValue),
            toDomainObject: dto => new LookupMusicbrainzAlbumTracksMessage(
                MessageId.From(dto.CommandId),
                CorrelationId.From(dto.CorrelationId),
                dto.CreatedAt,
                ParsePriority(dto.Priority),
                AlbumId.From(dto.AlbumId ?? throw new InvalidOperationException("Album id is required."))));

        registry.RegisterPair<LookupStreamingLocationByIsrcMessage, StreamingLocationLookupCommandDto>(
            toDto: message => new StreamingLocationLookupCommandDto(
                message.Id.Value,
                message.CorrelationId.Value,
                message.CreatedAt,
                ToDto(message.Priority),
                "isrc",
                message.TrackId.Value,
                message.Provider.Value),
            toDomainObject: dto => new LookupStreamingLocationByIsrcMessage(
                MessageId.From(dto.CommandId),
                CorrelationId.From(dto.CorrelationId),
                dto.CreatedAt,
                ParsePriority(dto.Priority),
                TrackId.From(dto.TrackId),
                ProviderName.From(dto.Provider)));

        registry.RegisterPair<LookupStreamingLocationByTrackMetadataMessage, StreamingLocationLookupCommandDto>(
            toDto: message => new StreamingLocationLookupCommandDto(
                message.Id.Value,
                message.CorrelationId.Value,
                message.CreatedAt,
                ToDto(message.Priority),
                "track-metadata",
                message.TrackId.Value,
                message.Provider.Value),
            toDomainObject: dto => new LookupStreamingLocationByTrackMetadataMessage(
                MessageId.From(dto.CommandId),
                CorrelationId.From(dto.CorrelationId),
                dto.CreatedAt,
                ParsePriority(dto.Priority),
                TrackId.From(dto.TrackId),
                ProviderName.From(dto.Provider)));

        registry.RegisterPair<LookupPlaylistTracksByProviderMessage, PlaylistTracksLookupCommandDto>(
            toDto: message => new PlaylistTracksLookupCommandDto(
                message.Id.Value,
                message.CorrelationId.Value,
                message.CreatedAt,
                ToDto(message.Priority),
                message.PlaylistId.Value,
                message.Provider.Value),
            toDomainObject: dto => new LookupPlaylistTracksByProviderMessage(
                MessageId.From(dto.CommandId),
                CorrelationId.From(dto.CorrelationId),
                dto.CreatedAt,
                ParsePriority(dto.Priority),
                PlaylistId.FromPlaylistName(dto.PlaylistId),
                ProviderName.From(dto.Provider)));

        registry.RegisterPair<CatalogLookupCompleted, CatalogLookupCompletedCommandDto>(
            toDto: message => ToDto(message),
            toDomainObject: dto => ToDomain(dto));

    }

    private static CatalogLookupCompletedCommandDto ToDto(CatalogLookupCompleted message)
    {
        var result = message.Result;
        var context = GetResultContext(result);
        return new CatalogLookupCompletedCommandDto(
            message.Id.Value,
            message.CorrelationId.Value,
            message.RequestedAt,
            GetResultKind(result),
            context.StreamId.StableValue,
            context.OriginalCommandId.Value,
            GetCompletedAt(result),
            GetReason(result),
            GetDeferredUntil(result),
            GetValue(result),
            GetExistingItem(result));
    }

    private static CatalogLookupCompleted ToDomain(CatalogLookupCompletedCommandDto dto)
    {
        var context = new LookupResultContext(
            new CatalogWorkId(dto.StreamId),
            MessageId.From(dto.OriginalCommandId));

        LookupResult result = dto.ResultKind switch
        {
            "succeeded" => new LookupResult.Succeeded(context, ParseValue(dto.Value), dto.CompletedAt),
            "duplicate" => new LookupResult.Duplicate(
                context,
                ParseCatalogItem(dto.ExistingItem ?? throw new InvalidOperationException("Existing item is required for duplicate lookup results.")),
                dto.Reason ?? string.Empty,
                dto.CompletedAt),
            "not-found" => new LookupResult.NotFound(context, dto.Reason ?? string.Empty, dto.CompletedAt),
            "deferred" => new LookupResult.Deferred(
                context,
                dto.DeferredUntil ?? dto.CompletedAt,
                dto.Reason ?? string.Empty,
                dto.CompletedAt),
            "failed" => new LookupResult.Failed(context, dto.Reason ?? string.Empty, dto.CompletedAt),
            _ => throw new InvalidOperationException($"Unsupported lookup result kind '{dto.ResultKind}'.")
        };

        return new CatalogLookupCompleted(
            MessageId.From(dto.CommandId),
            dto.RequestedAt,
            CorrelationId.From(dto.CorrelationId),
            result);
    }

    private static CatalogLookupValueDto? GetValue(LookupResult result) =>
        result.Match(
            succeeded => succeeded.Value switch
            {
                LookedUpData.CatalogEntries(var values) => new CatalogLookupValueDto(
                    "catalog-entries",
                    values.Select(entry => new CatalogDiscoveryEntryCommandDto(entry.ArtistId.Value, ToDto(entry.Item))).ToArray(),
                    null,
                    null),
                LookedUpData.PlaylistTrackReferences(var values) => new CatalogLookupValueDto(
                    "playlist-track-references",
                    null,
                    values.Select(value => new TrackReferenceCommandDto(value.ArtistName.Value, value.TrackTitle)).ToArray(),
                    null),
                LookedUpData.TrackStreamingLink(var artistId, var trackId, var value) => new CatalogLookupValueDto(
                    "track-streaming-link",
                    null,
                    null,
                    new TrackStreamingLinkCommandDto(
                        artistId.Value,
                        trackId.Value,
                        value.Provider.Value,
                        value.ExternalId,
                        value.Url.ToString(),
                        value.SourceProvider.Value,
                        value.ObservedAt)),
                _ => throw new InvalidOperationException($"Unsupported looked up data '{succeeded.Value.GetType().Name}'.")
            },
            _ => null,
            _ => null,
            _ => null,
            _ => null);

    private static LookedUpData ParseValue(CatalogLookupValueDto? dto) =>
        dto?.ValueKind switch
        {
            "catalog-entries" => new LookedUpData.CatalogEntries(
                (dto.CatalogEntries ?? []).Select(entry => new CatalogDiscoveryEntry(ArtistId.From(entry.ArtistId), ParseCatalogItem(entry.Item))).ToArray()),
            "playlist-track-references" => new LookedUpData.PlaylistTrackReferences(
                (dto.PlaylistTrackReferences ?? []).Select(entry => new TrackReference(ArtistName.From(entry.ArtistName), entry.TrackTitle)).ToArray()),
            "track-streaming-link" => ParseTrackStreamingLink(dto.TrackStreamingLink),
            _ => throw new InvalidOperationException($"Unsupported lookup value kind '{dto?.ValueKind}'.")
        };

    private static LookedUpData.TrackStreamingLink ParseTrackStreamingLink(TrackStreamingLinkCommandDto? dto)
    {
        if (dto is null)
        {
            throw new InvalidOperationException("Track streaming link payload is required.");
        }

        return new LookedUpData.TrackStreamingLink(
            ArtistId.From(dto.ArtistId),
            TrackId.From(dto.TrackId),
            new StreamingLocation(
                ProviderName.From(dto.Provider),
                dto.ExternalId,
                new Uri(dto.Url, UriKind.Absolute),
                LookupSource.From(dto.SourceProvider),
                dto.ObservedAt));
    }

    private static CatalogItemCommandDto? GetExistingItem(LookupResult result) =>
        result.Match(
            _ => null,
            duplicate => ToDto(duplicate.ExistingItem),
            _ => null,
            _ => null,
            _ => null);

    private static CatalogItemCommandDto ToDto(CatalogItem item) =>
        item switch
        {
            CatalogItem.MusicArtist(var artist) => new CatalogItemCommandDto(
                "artist",
                artist.Id.Value,
                artist.Name.Value,
                artist.ImageUrl,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null),
            CatalogItem.MusicAlbum(var album) => new CatalogItemCommandDto(
                "album",
                null,
                null,
                null,
                album.AlbumId.StableValue,
                album.AlbumTitle,
                SourceSystemIdSet.MusicBrainzIdOrNull(album.SourceSystemIds),
                album.ReleaseDate,
                album.ArtworkUrl,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                album.UpdatedAt,
                SourceSystemIdSet.ToStableValues(album.SourceSystemIds)),
            CatalogItem.MusicTrack(var track) => new CatalogItemCommandDto(
                "track",
                null,
                null,
                null,
                null,
                null,
                null,
                track.ReleaseDate,
                track.ArtworkUrl,
                track.TrackId.Value,
                track.Title,
                track.ArtistName,
                track.AlbumId,
                track.AlbumTitle,
                track.DurationMs,
                track.Isrc,
                SourceSystemIdSet.MusicBrainzIdOrNull(track.SourceSystemIds),
                track.ReleaseType,
                track.UpdatedAt,
                SourceSystemIdSet.ToStableValues(track.SourceSystemIds)),
            _ => throw new InvalidOperationException($"Unsupported catalog item '{item.GetType().Name}'.")
        };

    private static CatalogItem ParseCatalogItem(CatalogItemCommandDto dto) =>
        dto.Kind switch
        {
            "artist" => new CatalogItem.MusicArtist(
                new Artist
                {
                    Id = ArtistId.From(dto.ArtistId ?? throw new InvalidOperationException("Artist id is required.")),
                    Name = ArtistName.From(dto.ArtistName),
                    ImageUrl = dto.ArtistImageUrl
                }),
            "album" => new CatalogItem.MusicAlbum(
                new Album(
                    AlbumId.From(dto.AlbumId ?? throw new InvalidOperationException("Album id is required.")),
                    dto.AlbumTitle,
                    ResolveCatalogItemSourceSystemIds(dto.SourceSystemIds, dto.SourceAlbumId),
                    dto.ReleaseDate,
                    dto.ArtworkUrl,
                    dto.UpdatedAt ?? DateTimeOffset.UtcNow)),
            "track" => new CatalogItem.MusicTrack(
                CreateTrackFromDto(dto)),
            _ => throw new InvalidOperationException($"Unsupported catalog item kind '{dto.Kind}'.")
        };

    private static string GetResultKind(LookupResult result) =>
        result.Match(
            _ => "succeeded",
            _ => "duplicate",
            _ => "not-found",
            _ => "deferred",
            _ => "failed");

    private static LookupResultContext GetResultContext(LookupResult result) =>
        result.Match(
            succeeded => succeeded.Context,
            duplicate => duplicate.Context,
            notFound => notFound.Context,
            deferred => deferred.Context,
            failed => failed.Context);

    private static DateTimeOffset GetCompletedAt(LookupResult result) =>
        result.Match(
            succeeded => succeeded.CompletedAt,
            duplicate => duplicate.CompletedAt,
            notFound => notFound.CompletedAt,
            deferred => deferred.CompletedAt,
            failed => failed.CompletedAt);

    private static string? GetReason(LookupResult result) =>
        result.Match(
            _ => null,
            duplicate => duplicate.Reason,
            notFound => notFound.Reason,
            deferred => deferred.Reason,
            failed => failed.Reason);

    private static DateTimeOffset? GetDeferredUntil(LookupResult result) =>
        result.Match(
            _ => (DateTimeOffset?)null,
            _ => (DateTimeOffset?)null,
            _ => (DateTimeOffset?)null,
            deferred => deferred.DeferredUntil,
            _ => (DateTimeOffset?)null);

    private static LookupPriorityBandDto ToDto(LookupPriorityBand priority) =>
        priority switch
        {
            LookupPriorityBand.Low => LookupPriorityBandDto.Low,
            LookupPriorityBand.High => LookupPriorityBandDto.High,
            _ => throw new InvalidOperationException($"Unsupported lookup priority '{priority}'.")
        };

    private static LookupPriorityBand ParsePriority(LookupPriorityBandDto priority) =>
        priority switch
        {
            LookupPriorityBandDto.Low => LookupPriorityBand.Low,
            LookupPriorityBandDto.High => LookupPriorityBand.High,
            _ => throw new InvalidOperationException($"Unsupported lookup priority DTO '{priority}'.")
        };

    private static CatalogItemKindDto GetTargetItemKind(EnrichmentTarget target) =>
        target switch
        {
            EnrichmentTarget.KnownCatalogItemOperation(CatalogItemOperation.StreamingLocationForTrack) => CatalogItemKindDto.Track,
            EnrichmentTarget.KnownCatalogItemOperation(CatalogItemOperation.ChildAlbumsForArtist) => CatalogItemKindDto.Artist,
            EnrichmentTarget.KnownCatalogItemOperation(CatalogItemOperation.ChildTracksForArtist) => CatalogItemKindDto.Artist,
            EnrichmentTarget.KnownCatalogItemOperation(CatalogItemOperation.ChildTracksForAlbum) => CatalogItemKindDto.Album,
            EnrichmentTarget.KnownCatalogItemOperation(CatalogItemOperation.ChildTracksForPlaylist) => CatalogItemKindDto.Track,
            EnrichmentTarget.SearchForUnknownCatalogItem(var criteria) => GetCatalogItemKind(criteria.SearchTypes),
            _ => throw new InvalidOperationException($"Unsupported enrichment target '{target.GetType().Name}'.")
        };

    private static string GetTargetItemValue(EnrichmentTarget target) =>
        target switch
        {
            EnrichmentTarget.KnownCatalogItemOperation(CatalogItemOperation.StreamingLocationForTrack(var trackId)) => trackId.Value,
            EnrichmentTarget.KnownCatalogItemOperation(CatalogItemOperation.ChildAlbumsForArtist(var artistId)) => artistId.Value,
            EnrichmentTarget.KnownCatalogItemOperation(CatalogItemOperation.ChildTracksForArtist(var artistId)) => artistId.Value,
            EnrichmentTarget.KnownCatalogItemOperation(CatalogItemOperation.ChildTracksForAlbum(var albumId)) => albumId.StableValue,
            EnrichmentTarget.KnownCatalogItemOperation(CatalogItemOperation.ChildTracksForPlaylist(var playlistId)) => playlistId.Value,
            EnrichmentTarget.SearchForUnknownCatalogItem(var criteria) => criteria.NormalisedIdentifier,
            _ => throw new InvalidOperationException($"Unsupported enrichment target '{target.GetType().Name}'.")
        };

    private static CatalogItemResourceKindDto GetTargetResourceKind(EnrichmentTarget target) =>
        target switch
        {
            EnrichmentTarget.KnownCatalogItemOperation => CatalogItemResourceKindDto.CatalogItemId,
            EnrichmentTarget.SearchForUnknownCatalogItem => CatalogItemResourceKindDto.SearchCriteria,
            _ => throw new InvalidOperationException($"Unsupported enrichment target '{target.GetType().Name}'.")
        };

    private static string GetTargetResourceValue(EnrichmentTarget target) =>
        target switch
        {
            EnrichmentTarget.KnownCatalogItemOperation(var operation) => operation.StableIdentifier(),
            EnrichmentTarget.SearchForUnknownCatalogItem(var criteria) => criteria.Query,
            _ => throw new InvalidOperationException($"Unsupported enrichment target '{target.GetType().Name}'.")
        };

    private static CatalogItemKindDto? GetTargetResourceItemKind(EnrichmentTarget target) =>
        target switch
        {
            EnrichmentTarget.KnownCatalogItemOperation(CatalogItemOperation.StreamingLocationForTrack) => CatalogItemKindDto.Track,
            EnrichmentTarget.KnownCatalogItemOperation(CatalogItemOperation.ChildAlbumsForArtist) => CatalogItemKindDto.Artist,
            EnrichmentTarget.KnownCatalogItemOperation(CatalogItemOperation.ChildTracksForArtist) => CatalogItemKindDto.Artist,
            EnrichmentTarget.KnownCatalogItemOperation(CatalogItemOperation.ChildTracksForAlbum) => CatalogItemKindDto.Album,
            _ => null
        };

    private static int? GetSearchTypesOrNull(EnrichmentTarget target) =>
        target switch
        {
            EnrichmentTarget.SearchForUnknownCatalogItem(var criteria) => (int)criteria.SearchTypes,
            _ => null
        };

    private static EnrichmentTarget ParseTarget(
        CatalogItemKindDto itemKind,
        string itemValue,
        CatalogItemResourceKindDto resourceKind,
        string resourceValue,
        CatalogItemKindDto? resourceItemKind,
        int? searchTypes = null)
    {
        if (resourceKind == CatalogItemResourceKindDto.SearchCriteria)
        {
            return new EnrichmentTarget.SearchForUnknownCatalogItem(
                new SearchCriteria(resourceValue, ParseSearchType(searchTypes, itemKind)));
        }

        if (resourceValue.StartsWith("child_tracks_for_playlist", StringComparison.Ordinal))
        {
            return new EnrichmentTarget.KnownCatalogItemOperation(
                new CatalogItemOperation.ChildTracksForPlaylist(
                    PlaylistId.FromPlaylistName(resourceValue.Contains(':', StringComparison.Ordinal)
                        ? resourceValue[(resourceValue.IndexOf(':') + 1)..]
                        : itemValue)));
        }

        return new EnrichmentTarget.KnownCatalogItemOperation(ParseOperation(resourceValue, resourceItemKind ?? itemKind, itemValue));
    }

    private static CatalogItemKindDto GetCatalogItemKind(SearchType searchType) =>
        searchType switch
        {
            SearchType.Artist => CatalogItemKindDto.Artist,
            SearchType.Album => CatalogItemKindDto.Album,
            SearchType.Track => CatalogItemKindDto.Track,
            SearchType.All => CatalogItemKindDto.Track,
            _ => throw new InvalidOperationException($"Unsupported search type '{searchType}'.")
        };

    private static SearchType ParseSearchType(int? searchTypes, CatalogItemKindDto itemKind)
    {
        if (searchTypes.HasValue)
        {
            return (SearchType)searchTypes.Value;
        }

        return itemKind switch
        {
            CatalogItemKindDto.Artist => SearchType.Artist,
            CatalogItemKindDto.Album => SearchType.Album,
            CatalogItemKindDto.Track => SearchType.Track,
            _ => throw new InvalidOperationException($"Unsupported catalog item kind DTO '{itemKind}'.")
        };
    }

    private static string GetTargetKind(EnrichmentTarget target) =>
        target switch
        {
            EnrichmentTarget.SearchForUnknownCatalogItem => "search",
            EnrichmentTarget.KnownCatalogItemOperation(var operation) => operation switch
            {
                CatalogItemOperation.StreamingLocationForTrack => "streaming_location_for_track",
                CatalogItemOperation.ChildAlbumsForArtist => "child_albums_for_artist",
                CatalogItemOperation.ChildTracksForArtist => "child_tracks_for_artist",
                CatalogItemOperation.ChildTracksForAlbum => "child_tracks_for_album",
                CatalogItemOperation.ChildTracksForPlaylist => "child_tracks_for_playlist",
                _ => throw new InvalidOperationException($"Unsupported catalog operation '{operation.GetType().Name}'.")
            },
            _ => throw new InvalidOperationException($"Unsupported enrichment target '{target.GetType().Name}'.")
        };

    private static string GetTargetValue(EnrichmentTarget target) =>
        target switch
        {
            EnrichmentTarget.SearchForUnknownCatalogItem(var criteria) => criteria.Query,
            EnrichmentTarget.KnownCatalogItemOperation(CatalogItemOperation.StreamingLocationForTrack(var trackId)) => trackId.Value,
            EnrichmentTarget.KnownCatalogItemOperation(CatalogItemOperation.ChildAlbumsForArtist(var artistId)) => artistId.Value,
            EnrichmentTarget.KnownCatalogItemOperation(CatalogItemOperation.ChildTracksForArtist(var artistId)) => artistId.Value,
            EnrichmentTarget.KnownCatalogItemOperation(CatalogItemOperation.ChildTracksForAlbum(var albumId)) => albumId.StableValue,
            EnrichmentTarget.KnownCatalogItemOperation(CatalogItemOperation.ChildTracksForPlaylist(var playlistId)) => playlistId.Value,
            _ => throw new InvalidOperationException($"Unsupported enrichment target '{target.GetType().Name}'.")
        };

    private static string? GetTargetItemKindOrNull(EnrichmentTarget target) =>
        target switch
        {
            EnrichmentTarget.KnownCatalogItemOperation(CatalogItemOperation.StreamingLocationForTrack) => "track",
            EnrichmentTarget.KnownCatalogItemOperation(CatalogItemOperation.ChildAlbumsForArtist) => "artist",
            EnrichmentTarget.KnownCatalogItemOperation(CatalogItemOperation.ChildTracksForArtist) => "artist",
            EnrichmentTarget.KnownCatalogItemOperation(CatalogItemOperation.ChildTracksForAlbum) => "album",
            EnrichmentTarget.KnownCatalogItemOperation(CatalogItemOperation.ChildTracksForPlaylist) => "playlist",
            _ => null
        };

    private static int GetSearchTypes(EnrichmentTarget target) =>
        target switch
        {
            EnrichmentTarget.SearchForUnknownCatalogItem(var criteria) => (int)criteria.SearchTypes,
            _ => 0
        };

    private static EnrichmentTarget ParseTarget(string kind, string value, string? itemKind, int searchTypes) =>
        kind switch
        {
            "search" => new EnrichmentTarget.SearchForUnknownCatalogItem(new SearchCriteria(value, (SearchType)searchTypes)),
            "child_tracks_for_playlist" => new EnrichmentTarget.KnownCatalogItemOperation(
                new CatalogItemOperation.ChildTracksForPlaylist(PlaylistId.FromPlaylistName(value))),
            _ => new EnrichmentTarget.KnownCatalogItemOperation(ParseOperation(
                kind,
                ParseCatalogItemKind(itemKind ?? throw new InvalidOperationException("Target item kind is required.")),
                value))
        };

    private static CatalogItemKindDto ParseCatalogItemKind(string itemKind) =>
        itemKind switch
        {
            "artist" => CatalogItemKindDto.Artist,
            "album" => CatalogItemKindDto.Album,
            "track" => CatalogItemKindDto.Track,
            _ => throw new InvalidOperationException($"Unsupported catalog item kind '{itemKind}'.")
        };

    private static CatalogItemOperation ParseOperation(string kind, CatalogItemKindDto itemKind, string value) =>
        (kind.Contains(':', StringComparison.Ordinal) ? kind[..kind.IndexOf(':')] : kind) switch
        {
            "streaming_location_for_track" when itemKind == CatalogItemKindDto.Track => new CatalogItemOperation.StreamingLocationForTrack(
                TrackId.From(kind.Contains(':', StringComparison.Ordinal) ? kind[(kind.IndexOf(':') + 1)..] : value)),
            "child_albums_for_artist" when itemKind == CatalogItemKindDto.Artist => new CatalogItemOperation.ChildAlbumsForArtist(
                ArtistId.From(kind.Contains(':', StringComparison.Ordinal) ? kind[(kind.IndexOf(':') + 1)..] : value)),
            "child_tracks_for_artist" when itemKind == CatalogItemKindDto.Artist => new CatalogItemOperation.ChildTracksForArtist(
                ArtistId.From(kind.Contains(':', StringComparison.Ordinal) ? kind[(kind.IndexOf(':') + 1)..] : value)),
            "child_tracks_for_album" when itemKind == CatalogItemKindDto.Album => new CatalogItemOperation.ChildTracksForAlbum(
                AlbumId.From(kind.Contains(':', StringComparison.Ordinal) ? kind[(kind.IndexOf(':') + 1)..] : value)),
            "child_tracks_for_playlist" => new CatalogItemOperation.ChildTracksForPlaylist(
                PlaylistId.FromPlaylistName(kind.Contains(':', StringComparison.Ordinal) ? kind[(kind.IndexOf(':') + 1)..] : value)),
            _ => throw new InvalidOperationException($"Unsupported catalog item operation '{kind}' with item kind '{itemKind}'.")
        };

    private static Track CreateTrackFromDto(CatalogItemCommandDto dto)
    {
        var track = new Track(TrackId.From(dto.TrackId ?? throw new InvalidOperationException("Track id is required.")))
        {
            Title = dto.TrackTitle ?? string.Empty,
            ArtistName = dto.TrackArtistName ?? string.Empty,
            AlbumId = dto.TrackAlbumId,
            AlbumTitle = dto.TrackAlbumTitle,
            DurationMs = dto.DurationMs,
            Isrc = dto.Isrc,
            ReleaseDate = dto.ReleaseDate,
            ReleaseType = dto.ReleaseType,
            ArtworkUrl = dto.ArtworkUrl,
            UpdatedAt = dto.UpdatedAt ?? DateTimeOffset.UtcNow
        };
        SourceSystemIdSet.UnionWith(
            track.SourceSystemIds,
            ResolveCatalogItemSourceSystemIds(dto.SourceSystemIds, dto.Mbid));
        return track;
    }

    private static HashSet<SourceSystemId> ResolveCatalogItemSourceSystemIds(
        IReadOnlyList<string>? sourceSystemIds,
        string? legacyMusicBrainzId)
    {
        var set = SourceSystemIdSet.FromStableValues(sourceSystemIds);
        if (set.Count == 0 && !string.IsNullOrWhiteSpace(legacyMusicBrainzId))
        {
            set.Add(SourceSystemId.MusicBrainz(legacyMusicBrainzId));
        }

        return set;
    }
}
