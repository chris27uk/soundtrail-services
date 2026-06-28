using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Domain.Enrichment.Commands;
using Soundtrail.Adapters.Registry;

namespace Soundtrail.Adapters.Messaging.Registrations;

public sealed class LookupTrackMetadataCommandTranslationRegistration : ITypeTranslationRegistration
{
    public void Register(TypeTranslationRegistry registry)
    {
        registry.Register<LookupTrackMetadataCommand, LookupTrackMetadataCommandDto>(
            translate: command =>
                new LookupTrackMetadataCommandDto(
                    command.CommandId.Value,
                    command.MusicCatalogId.Value,
                    command.Priority,
                    command.CreatedAt,
                    command.CorrelationId.Value,
                    command.SearchCriteria.Kind,
                    command.SearchCriteria.UnifiedQuery,
                    command.SearchCriteria.Isrc,
                    command.SearchCriteria.Title,
                    command.SearchCriteria.Artist,
                    command.SearchCriteria.Album,
                    command.Hierarchy?.ArtistId?.Value,
                    command.Hierarchy?.AlbumId?.Value));
    }
}
