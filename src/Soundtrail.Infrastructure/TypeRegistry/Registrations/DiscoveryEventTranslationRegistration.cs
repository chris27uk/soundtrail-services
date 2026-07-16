using Soundtrail.Adapters.Registry;
using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Albums;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Events;
using Soundtrail.Domain.Search;

namespace Soundtrail.Adapters.TypeRegistry.Registrations;

public sealed class DiscoveryEventTranslationRegistration : ITypeTranslationRegistration
{
    public void Register(TypeTranslationRegistry registry)
    {
        registry.RegisterStoredEventPair<WorkRequested, CatalogDiscoveryWorkRequestedEventDataRecordDto>(
            eventType: "work-requested",
            toDto: @event => new CatalogDiscoveryWorkRequestedEventDataRecordDto(
                GetResourceKind(@event.Target),
                GetResourceValue(@event.Target),
                GetResourceItemKind(@event.Target),
                @event.TrustLevel,
                @event.RiskScore,
                @event.RequestedAt,
                @event.CorrelationId.Value),
            toDomainObject: dto => new WorkRequested(
                ParseFilter(dto.ResourceKind, dto.ResourceValue, dto.ResourceItemKind),
                dto.TrustLevel,
                dto.RiskScore,
                dto.RequestedAtUtc,
                CorrelationId.From(dto.CorrelationId)),
            occurredAtUtc: @event => @event.RequestedAt,
            correlationId: @event => @event.CorrelationId.Value);
    }

    private static string GetResourceKind(EnrichmentTarget target) =>
        target switch
        {
            EnrichmentTarget.Unknown => "search-criteria",
            EnrichmentTarget.Existing => "catalog-item",
            _ => throw new InvalidOperationException($"Unsupported enrichment filter '{target.GetType().Name}'.")
        };

    private static string GetResourceValue(EnrichmentTarget target) =>
        target switch
        {
            EnrichmentTarget.Unknown(var searchCriteria) => searchCriteria.Query,
            EnrichmentTarget.Existing(var itemId) => itemId.NormalisedIdentifier,
            _ => throw new InvalidOperationException($"Unsupported enrichment filter '{target.GetType().Name}'.")
        };

    private static string? GetResourceItemKind(EnrichmentTarget target) =>
        target switch
        {
            EnrichmentTarget.Existing(var itemId) => itemId switch
            {
                CatalogItemId.Track => "track",
                CatalogItemId.Artist => "artist",
                CatalogItemId.Album => "album",
                CatalogItemId.Playlist => "playlist",
                _ => throw new InvalidOperationException($"Unsupported catalog item id '{itemId.GetType().Name}'.")
            },
            _ => null
        };

    private static EnrichmentTarget ParseFilter(string resourceKind, string resourceValue, string? resourceItemKind) =>
        resourceKind switch
        {
            "search-criteria" => new EnrichmentTarget.Unknown(new SearchCriteria(resourceValue)),
            "catalog-item" => new EnrichmentTarget.Existing(ParseCatalogItemId(resourceValue, resourceItemKind)),
            _ => throw new InvalidOperationException($"Unsupported resource kind '{resourceKind}'.")
        };

    private static CatalogItemId ParseCatalogItemId(string resourceValue, string? resourceItemKind) =>
        resourceItemKind switch
        {
            "track" => new CatalogItemId.Track(TrackId.From(resourceValue)),
            "artist" => new CatalogItemId.Artist(ArtistId.From(resourceValue)),
            "album" => new CatalogItemId.Album(AlbumId.From(resourceValue)),
            _ => throw new InvalidOperationException($"Unsupported resource item kind '{resourceItemKind}'.")
        };
}
