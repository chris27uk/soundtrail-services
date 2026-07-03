using Soundtrail.Adapters.Registry;
using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.IntegrationMessaging.Responses;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Enrichment.Responses;
using Soundtrail.Domain.Search;

namespace Soundtrail.Adapters.Messaging.Registrations;

public sealed class CatalogItemLookupAttemptedTranslationRegistration : ITypeTranslationRegistration
{
    public void Register(TypeTranslationRegistry registry)
    {
        registry.RegisterPair<CatalogItemLookupAttempted, CatalogItemLookupAttemptedDto>(
            attempted =>
            {
                var (trackFetched, artistFetched, albumFetched) = ToDtos(attempted.Content);

                return new CatalogItemLookupAttemptedDto(
                    attempted.CommandId.Value,
                    ToDtoKind(attempted.ItemId.EntityKind),
                    attempted.ItemId.StableValue,
                    attempted.SourceProvider.Value,
                    attempted.Priority,
                    attempted.CreatedAt,
                    attempted.CorrelationId.Value,
                    new MusicCatalogLookupOutcomeDto(
                        attempted.Outcome.Status.ToString(),
                        attempted.Outcome.Reason,
                        attempted.Outcome.RetryAt,
                        attempted.Outcome.RetryAfterSeconds),
                    trackFetched,
                    artistFetched,
                    albumFetched,
                    attempted.SearchCriteria is null ? null : DiscoveryQueryKey.StableValueFor(attempted.SearchCriteria));
            },
            dto => new CatalogItemLookupAttempted(
                CommandId.From(dto.CommandId),
                ToDomainItemId(dto.ItemKind, dto.ItemValue),
                LookupSource.From(dto.SourceProvider),
                dto.Priority,
                dto.CreatedAt,
                CorrelationId.From(dto.CorrelationId),
                new MusicCatalogLookupOutcome(
                    Enum.Parse<MusicCatalogLookupOutcomeStatus>(dto.Outcome.Status),
                    dto.Outcome.Reason,
                    dto.Outcome.RetryAt,
                    dto.Outcome.RetryAfterSeconds),
                ToDomainContent(dto),
                string.IsNullOrWhiteSpace(dto.SearchCriteria) ? null : DiscoveryQueryKey.ToMusicSearchCriteria(dto.SearchCriteria)));
    }

    private static (MusicCatalogMetadataFetchedDto? Track, ArtistMetadataFetchedDto? Artist, AlbumMetadataFetchedDto? Album) ToDtos(CatalogItemLookupContent? content) =>
        content switch
        {
            CatalogItemLookupContent.Track(var fetched) => (ToTrackDto(fetched), null, null),
            CatalogItemLookupContent.Artist(var fetched) => (null, ToArtistDto(fetched), null),
            CatalogItemLookupContent.Album(var fetched) => (null, null, ToAlbumDto(fetched)),
            null => (null, null, null),
            _ => throw new InvalidOperationException($"Unsupported catalog item lookup content type '{content.GetType().Name}'.")
        };

    private static CatalogItemLookupContent? ToDomainContent(CatalogItemLookupAttemptedDto dto)
    {
        if (dto.MusicCatalogMetadataFetched is not null)
        {
            return new CatalogItemLookupContent.Track(ToTrackFetched(dto.MusicCatalogMetadataFetched));
        }

        if (dto.ArtistMetadataFetched is not null)
        {
            return new CatalogItemLookupContent.Artist(ToArtistFetched(dto.ArtistMetadataFetched));
        }

        if (dto.AlbumMetadataFetched is not null)
        {
            return new CatalogItemLookupContent.Album(ToAlbumFetched(dto.AlbumMetadataFetched));
        }

        return null;
    }

    private static MusicCatalogMetadataFetchedDto ToTrackDto(MusicCatalogMetadataFetched fetched) =>
        new(
            fetched.CommandId.Value,
            fetched.MusicCatalogId.Value,
            fetched.SourceProvider.Value,
            fetched.Priority,
            fetched.CreatedAt,
            fetched.Metadata is null
                ? null
                : new SongMetadataDto(
                    fetched.Metadata.Title,
                    fetched.Metadata.Artist,
                    fetched.Metadata.Isrc,
                    fetched.Metadata.Mbid,
                    fetched.Metadata.DurationMs,
                    fetched.Metadata.AlbumTitle,
                    fetched.Metadata.ReleaseDate,
                    fetched.Metadata.SourceArtistId,
                    fetched.Metadata.SourceAlbumId),
            fetched.References.Select(reference => new ExternalReferenceDto(
                reference.Provider.Value,
                reference.Url,
                reference.ExternalId)).ToArray(),
            fetched.FailedProviders.Select(failure => new ProviderLookupFailureDto(
                failure.Provider.Value,
                failure.SourceProvider.Value)).ToArray(),
            fetched.Hierarchy?.ArtistId?.Value,
            fetched.Hierarchy?.AlbumId?.Value,
            fetched.CorrelationId.Value);

    private static ArtistMetadataFetchedDto ToArtistDto(ArtistMetadataFetched fetched) =>
        new(
            fetched.CommandId.Value,
            fetched.ArtistId.Value,
            fetched.SourceProvider.Value,
            fetched.Priority,
            fetched.CreatedAt,
            new ArtistMetadataDto(
                fetched.Metadata.ArtistName,
                fetched.Metadata.SourceArtistId),
            fetched.CorrelationId.Value);

    private static AlbumMetadataFetchedDto ToAlbumDto(AlbumMetadataFetched fetched) =>
        new(
            fetched.CommandId.Value,
            fetched.ArtistId.Value,
            fetched.AlbumId.Value,
            fetched.SourceProvider.Value,
            fetched.Priority,
            fetched.CreatedAt,
            new AlbumMetadataDto(
                fetched.Metadata.AlbumTitle,
                fetched.Metadata.ArtistName,
                fetched.Metadata.SourceAlbumId,
                fetched.Metadata.SourceArtistId,
                fetched.Metadata.ReleaseDate),
            fetched.CorrelationId.Value);

    private static MusicCatalogMetadataFetched ToTrackFetched(MusicCatalogMetadataFetchedDto dto) =>
        new(
            CommandId.From(dto.CommandId),
            MusicCatalogId.From(dto.MusicCatalogId),
            LookupSource.From(dto.SourceProvider),
            dto.Priority,
            dto.CreatedAt,
            dto.Metadata is null
                ? null
                : new SongMetadata(
                    dto.Metadata.Title,
                    dto.Metadata.Artist,
                    dto.Metadata.Isrc,
                    dto.Metadata.Mbid,
                    dto.Metadata.DurationMs,
                    dto.Metadata.AlbumTitle,
                    dto.Metadata.ReleaseDate,
                    dto.Metadata.SourceArtistId,
                    dto.Metadata.SourceAlbumId),
            dto.References.Select(reference => new ExternalReference(
                ProviderName.From(reference.Provider),
                reference.Url,
                reference.ExternalId)).ToArray(),
            dto.FailedProviders.Select(failure => new ProviderLookupFailure(
                ProviderName.From(failure.Provider),
                LookupSource.From(failure.SourceProvider))).ToArray(),
            dto.ArtistId is null && dto.AlbumId is null
                ? null
                : new CatalogTrackHierarchy(
                    dto.ArtistId is null ? null : ArtistId.From(dto.ArtistId),
                    dto.AlbumId is null ? null : AlbumId.From(dto.AlbumId)),
            CorrelationId.From(dto.CorrelationId));

    private static ArtistMetadataFetched ToArtistFetched(ArtistMetadataFetchedDto dto) =>
        new(
            CommandId.From(dto.CommandId),
            ArtistId.From(dto.ArtistId),
            LookupSource.From(dto.SourceProvider),
            dto.Priority,
            dto.CreatedAt,
            new ArtistMetadata(
                dto.Metadata.ArtistName,
                dto.Metadata.SourceArtistId),
            CorrelationId.From(dto.CorrelationId));

    private static AlbumMetadataFetched ToAlbumFetched(AlbumMetadataFetchedDto dto) =>
        new(
            CommandId.From(dto.CommandId),
            ArtistId.From(dto.ArtistId),
            AlbumId.From(dto.AlbumId),
            LookupSource.From(dto.SourceProvider),
            dto.Priority,
            dto.CreatedAt,
            new AlbumMetadata(
                dto.Metadata.AlbumTitle,
                dto.Metadata.ArtistName,
                dto.Metadata.SourceAlbumId,
                dto.Metadata.SourceArtistId,
                dto.Metadata.ReleaseDate),
            CorrelationId.From(dto.CorrelationId));

    private static CatalogItemKind ToDtoKind(CatalogEntityKind entityKind) =>
        entityKind switch
        {
            CatalogEntityKind.Track => CatalogItemKind.Track,
            CatalogEntityKind.Artist => CatalogItemKind.Artist,
            CatalogEntityKind.Album => CatalogItemKind.Album,
            _ => throw new InvalidOperationException($"Unsupported catalog entity kind '{entityKind}'.")
        };

    private static CatalogItemId ToDomainItemId(CatalogItemKind itemKind, string itemValue) =>
        itemKind switch
        {
            CatalogItemKind.Track => new CatalogItemId.Track(TrackId.From(itemValue)),
            CatalogItemKind.Artist => new CatalogItemId.Artist(ArtistId.From(itemValue)),
            CatalogItemKind.Album => new CatalogItemId.Album(CatalogAlbumId.Parse(itemValue)),
            _ => throw new InvalidOperationException($"Unsupported catalog item kind '{itemKind}'.")
        };
}
