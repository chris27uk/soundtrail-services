using Soundtrail.Adapters.Registry;
using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Contracts.IntegrationMessaging.Responses;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Enrichment.Events;
using Soundtrail.Domain.Enrichment.Responses;
using Soundtrail.Domain.Search;

namespace Soundtrail.Adapters.Enrichment;

public sealed class MusicCatalogLookupStoredEventTranslator : ITypeTranslationRegistration
{
    public void Register(TypeTranslationRegistry registry)
    {
        registry.RegisterStoredEventPair<MusicCatalogLookupStarted, MusicCatalogLookupStartedEventDataRecordDto>(
            nameof(MusicCatalogLookupStarted),
            domainEvent => new MusicCatalogLookupStartedEventDataRecordDto(
                domainEvent.MusicCatalogId.Value,
                domainEvent.Priority.ToString(),
                domainEvent.StartedAt),
            dto => new MusicCatalogLookupStarted(
                MusicCatalogId.From(dto.MusicCatalogId),
                Enum.Parse<LookupPriorityBand>(dto.Priority, true),
                dto.StartedAtUtc),
            domainEvent => domainEvent.StartedAt);

        registry.RegisterStoredEventPair<MusicCatalogLookupCompleted, MusicCatalogLookupCompletedEventDataRecordDto>(
            nameof(MusicCatalogLookupCompleted),
            domainEvent => new MusicCatalogLookupCompletedEventDataRecordDto(
                domainEvent.MusicCatalogId.Value,
                domainEvent.SourceProvider.Value,
                domainEvent.Priority.ToString(),
                domainEvent.CompletedAt,
                domainEvent.Metadata is null
                    ? null
                    : new SongMetadataDto(
                        domainEvent.Metadata.Title,
                        domainEvent.Metadata.Artist,
                        domainEvent.Metadata.Isrc,
                        domainEvent.Metadata.Mbid,
                        domainEvent.Metadata.DurationMs,
                        domainEvent.Metadata.AlbumTitle,
                        domainEvent.Metadata.ReleaseDate,
                        domainEvent.Metadata.SourceArtistId,
                        domainEvent.Metadata.SourceAlbumId),
                domainEvent.References.Select(static reference =>
                    new ExternalReferenceDto(
                        reference.Provider.Value,
                        reference.Url,
                        reference.ExternalId)).ToArray(),
                domainEvent.FailedProviders.Select(static failure =>
                    new ProviderLookupFailureDto(
                        failure.Provider.Value,
                        failure.SourceProvider.Value)).ToArray(),
                domainEvent.Hierarchy?.ArtistId?.Value,
                domainEvent.Hierarchy?.AlbumId?.Value,
                domainEvent.SearchCriteria is null ? null : DiscoveryQueryKey.StableValueFor(domainEvent.SearchCriteria)),
            dto => new MusicCatalogLookupCompleted(
                MusicCatalogId.From(dto.MusicCatalogId),
                LookupSource.From(dto.SourceProvider),
                Enum.Parse<LookupPriorityBand>(dto.Priority, true),
                dto.CompletedAtUtc,
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
                dto.References.Select(static reference =>
                    new ExternalReference(
                        ProviderName.From(reference.Provider),
                        reference.Url,
                        reference.ExternalId)).ToArray(),
                dto.FailedProviders.Select(static failure =>
                    new ProviderLookupFailure(
                        ProviderName.From(failure.Provider),
                        LookupSource.From(failure.SourceProvider))).ToArray(),
                dto.ArtistId is null && dto.AlbumId is null
                    ? null
                    : new CatalogTrackHierarchy(
                        dto.ArtistId is null ? null : ArtistId.From(dto.ArtistId),
                        dto.AlbumId is null ? null : AlbumId.From(dto.AlbumId)),
                dto.SearchCriteriaValue is null ? null : DiscoveryQueryKey.ToMusicSearchCriteria(dto.SearchCriteriaValue)),
            domainEvent => domainEvent.CompletedAt);

        registry.RegisterStoredEventPair<MusicCatalogLookupDeferred, MusicCatalogLookupDeferredEventDataRecordDto>(
            nameof(MusicCatalogLookupDeferred),
            domainEvent => new MusicCatalogLookupDeferredEventDataRecordDto(
                domainEvent.MusicCatalogId.Value,
                domainEvent.Priority.ToString(),
                domainEvent.DeferredAt,
                domainEvent.RetryAfterSeconds,
                domainEvent.RetryAt,
                domainEvent.Reason,
                domainEvent.SearchCriteria is null ? null : DiscoveryQueryKey.StableValueFor(domainEvent.SearchCriteria)),
            dto => new MusicCatalogLookupDeferred(
                MusicCatalogId.From(dto.MusicCatalogId),
                Enum.Parse<LookupPriorityBand>(dto.Priority, true),
                dto.DeferredAtUtc,
                dto.RetryAfterSeconds,
                dto.RetryAtUtc,
                dto.Reason,
                dto.SearchCriteriaValue is null ? null : DiscoveryQueryKey.ToMusicSearchCriteria(dto.SearchCriteriaValue)),
            domainEvent => domainEvent.DeferredAt);

        registry.RegisterStoredEventPair<MusicCatalogLookupFailed, MusicCatalogLookupFailedEventDataRecordDto>(
            nameof(MusicCatalogLookupFailed),
            domainEvent => new MusicCatalogLookupFailedEventDataRecordDto(
                domainEvent.MusicCatalogId.Value,
                domainEvent.Priority.ToString(),
                domainEvent.FailedAt,
                domainEvent.Reason,
                domainEvent.SearchCriteria is null ? null : DiscoveryQueryKey.StableValueFor(domainEvent.SearchCriteria)),
            dto => new MusicCatalogLookupFailed(
                MusicCatalogId.From(dto.MusicCatalogId),
                Enum.Parse<LookupPriorityBand>(dto.Priority, true),
                dto.FailedAtUtc,
                dto.Reason,
                dto.SearchCriteriaValue is null ? null : DiscoveryQueryKey.ToMusicSearchCriteria(dto.SearchCriteriaValue)),
            domainEvent => domainEvent.FailedAt);
    }
}
