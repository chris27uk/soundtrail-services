using Soundtrail.Adapters.Registry;
using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Assesment;
using Soundtrail.Domain.Search;

namespace Soundtrail.Adapters.Messaging.Registrations;

public sealed class AssessMusicCatalogItemCommandTranslationRegistration : ITypeTranslationRegistration
{
    public void Register(TypeTranslationRegistry registry)
    {
        registry.RegisterPair<AssessWorkCommand, AssessMusicCatalogItemCommandDto>(
            command =>
            {
                var (resourceKind, resourceValue, resourceItemKind) = ToDtoResource(command.Query);

                return new AssessMusicCatalogItemCommandDto(
                    command.CommandId.Value,
                    command.CorrelationId.Value,
                    command.CreatedAt,
                    command.Priority,
                    ToDtoKind(command.ItemId.EntityKind),
                    command.ItemId.StableValue,
                    resourceKind,
                    resourceValue,
                    resourceItemKind,
                    command.TrustLevel,
                    command.RiskScore);
            },
            dto =>
                new AssessWorkCommand(
                    CommandId.For(dto.CommandId),
                    CorrelationId.From(dto.CorrelationId),
                    dto.CreatedAt,
                    dto.Priority,
                    ToDomainItemId(dto.ItemKind, dto.ItemValue),
                    ToDomainResource(dto.ResourceKind, dto.ResourceValue, dto.ResourceItemKind),
                    dto.TrustLevel,
                    dto.RiskScore));
    }

    private static (CatalogItemResourceKind ResourceKind, string ResourceValue, CatalogItemKind? ResourceItemKind) ToDtoResource(EnrichmentQuery resource) =>
        resource switch
        {
            EnrichmentQuery.SearchCriteria(var searchCriteria) => (
                CatalogItemResourceKind.SearchCriteria,
                DiscoveryQueryKey.StableValueFor(searchCriteria),
                null),
            EnrichmentQuery.CatalogItem(var itemId) => (
                CatalogItemResourceKind.CatalogItemId,
                itemId.StableValue,
                ToDtoKind(itemId.EntityKind)),
            _ => throw new InvalidOperationException($"Unsupported catalog item resource type '{resource.GetType().Name}'.")
        };

    private static EnrichmentQuery ToDomainResource(
        CatalogItemResourceKind resourceKind,
        string resourceValue,
        CatalogItemKind? resourceItemKind)
    {
        return resourceKind switch
        {
            CatalogItemResourceKind.SearchCriteria => EnrichmentQuery.ForSearch(DiscoveryQueryKey.ToMusicSearchCriteria(resourceValue)),
            CatalogItemResourceKind.CatalogItemId when resourceItemKind is not null =>
                EnrichmentQuery.ForCatalogItem(ToDomainItemId(resourceItemKind.Value, resourceValue)),
            CatalogItemResourceKind.CatalogItemId =>
                throw new InvalidOperationException("Catalog item resource kind requires a resource item kind."),
            _ => throw new InvalidOperationException($"Unsupported catalog item resource kind '{resourceKind}'.")
        };
    }

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
